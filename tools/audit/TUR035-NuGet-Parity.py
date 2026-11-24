"""TUR035: compare the original TUR033 artifact against exact official NuGet archives.
This is a read-only evidence collector, never release or legal approval.
"""
import base64, hashlib, json, pathlib, re, subprocess, urllib.request, zipfile, xml.etree.ElementTree as ET
from collections import Counter
ROOT=pathlib.Path("tur035-evidence")
ROOT.mkdir(exist_ok=True)
def sha(b): return hashlib.sha256(b).hexdigest()
def sha512(b): return base64.b64encode(hashlib.sha512(b).digest()).decode()
def get(url):
    if not url.startswith("https://api.nuget.org/"): raise ValueError("unexpected official endpoint")
    with urllib.request.urlopen(url,timeout=90) as r:
        if not r.url.startswith("https://api.nuget.org/"): raise ValueError("unexpected redirect")
        return r.read()
def save(path,data):
    p=ROOT/path; p.parent.mkdir(parents=True,exist_ok=True); p.write_bytes(data)
    return {"path":str(path),"sha256":sha(data),"bytes":len(data)}
baseline=pathlib.Path("tools/audit/TUR034-baseline.json").read_bytes()
print("TUR035 baseline bytes",len(baseline),"sha256",sha(baseline))
assert sha(baseline)=="fa7d0b4dc89c6265173cf88101bbd596110cdcb256a54cf4596347db9594f306"
d=json.loads(baseline)
originals=list(pathlib.Path("original").rglob("Embasic.Legal.exe"))
assert len(originals)==1, "ambiguous or absent original EXE"
original=originals[0].parent
exe=originals[0].read_bytes()
assert sha(exe)==d["exe_sha256"]=="262ac3abd82439e14add33807546bc3abbf15e5f9fb8fecae585d455f842be29"
prov=json.loads((original/"BUILD_PROVENANCE.json").read_text(encoding="utf-8-sig"))
assert prov["git_head_sha"]==prov["checkout_sha"]==d["exact_head"]
assert str(prov["run_id"] if "run_id" in prov else prov["github_run_id"])=="45342165417"
rows=d["rows"]
assert len(rows)==390 and len({r["path"] for r in rows})==390
# Validate previously recorded member bytes; not a new bundle inventory.
for r in rows:
    assert r["compressed_size"]==0
    assert sha(exe[r["offset"]:r["offset"]+r["size"]])==r["sha256"],r["path"]
result={"baseline_sha256":sha(baseline),"original_exe_sha256":sha(exe),"original_head":d["exact_head"],"original_artifact_id":14896671662,"packages":[],"member_results":[],"production":"NO-GO","physical_uat":"NOT_RUN","official_law":"NOT_VERIFIED","license_release":"NO-GO"}
for owner,lib in d["deps"]["libraries"].items():
    if lib["type"] not in ("package","runtimepack"): continue
    ident,ver=owner.removeprefix("runtimepack.").split("/")
    id=ident.lower()
    name=id+"."+ver
    rec={"owner":owner,"id":ident,"version":ver,"build_declared_content_sha512":lib.get("sha512","").removeprefix("sha512-"),"license_documents":[],"status":"NOT_VERIFIED"}
    result["packages"].append(rec)
    members=[r for r in rows if r["owner"]==owner]
    try:
        url=f"https://api.nuget.org/v3-flatcontainer/{id}/{ver}/{name}.nupkg"
        data=get(url)
        rec.update(url=url,bytes=len(data),archive_sha256=sha(data),archive_sha512_base64=sha512(data))
        local=pathlib.Path(name+".nupkg"); local.write_bytes(data)
        try:
            leaf=json.loads(get(f"https://api.nuget.org/v3/registration5-gz-semver2/{id}/{ver}.json"))
            entry=leaf["catalogEntry"]
            cat=json.loads(get(entry if isinstance(entry,str) else entry["@id"]))
            assert cat["id"].lower()==id and cat["version"].lower()==ver.lower()
            rec.update(catalog_hash=cat.get("packageHash"),catalog_algorithm=cat.get("packageHashAlgorithm"),catalog_size=cat.get("packageSize"))
            rec["catalog_archive_hash_match"]=cat.get("packageHashAlgorithm","").upper()=="SHA512" and cat.get("packageHash")==rec["archive_sha512_base64"] and cat.get("packageSize")==len(data)
        except Exception as e: rec["catalog_error"]=str(e)
        verify=subprocess.run(["dotnet","nuget","verify",str(local),"--all","--verbosity","detailed"],capture_output=True,text=True,encoding="utf-8",errors="replace",timeout=180)
        log=verify.stdout+"\n"+verify.stderr
        rec["signature_verify_exit"]=verify.returncode
        rec["verify_log"]=save(pathlib.Path("verification")/(name+".txt"),log.encode())
        # .NET 10 CLI prints package content hash, not necessarily raw signed-archive hash.
        hashes=re.findall(r"(?im)^\s*(?:package\s+)?content\s+hash\s*:\s*(?:sha512-)?([A-Za-z0-9+/]{86}==)\s*$",log)
        rec["nuget_content_sha512"]=hashes[0] if len(set(hashes))==1 else None
        rec["build_content_hash_match"]=(rec["nuget_content_sha512"]==rec["build_declared_content_sha512"]) if rec["build_declared_content_sha512"] and rec["nuget_content_sha512"] else None
        with zipfile.ZipFile(local) as z:
            assert z.testzip() is None
            entries=z.namelist()
            assert len(entries)==len(set(entries))
            nuspec=[p for p in entries if p.lower().endswith(".nuspec")]
            assert len(nuspec)==1
            root=ET.fromstring(z.read(nuspec[0]))
            meta=root.find("{*}metadata")
            assert meta.find("{*}id").text.lower()==id and meta.find("{*}version").text.lower()==ver.lower()
            lic=meta.find("{*}license")
            rec["nuspec_license"]={"type":lic.attrib.get("type"),"value":lic.text} if lic is not None else None
            docpaths={p for p in entries if re.search(r"(?i)(?:^|/)(?:license[^/]*|(?:third[-_ ]?party[-_ ]?)?notices?[^/]*|copying[^/]*)$",p) and not p.endswith("/")}
            if lic is not None and lic.attrib.get("type")=="file":
                docpaths.add(lic.text)
            for i,p in enumerate(sorted(docpaths)):
                if p in entries:
                    content=z.read(p)
                    # preserve exact bytes, using safe generated path
                    info=save(pathlib.Path("license-documents")/name/(f"{i:02d}-"+pathlib.PurePosixPath(p).name),content)
                    rec["license_documents"].append({"archive_path":p,**info})
                else: rec["missing_declared_license_file"]=p
            rec["status"]="ARCHIVE_INSPECTED"
            for r in members:
                target=r["path"].replace("\\","/").lower()
                candidates=[p for p in entries if p.lower().endswith("/"+target) or p.lower()==target]
                # Do not count reference assemblies or native libraries for non-win-x64 RIDs.
                candidates=[p for p in candidates if not p.lower().startswith("ref/") and (not p.lower().startswith("runtimes/") or p.lower().startswith("runtimes/win-x64/") or p.lower().startswith("runtimes/win/"))]
                matches=[p for p in candidates if sha(z.read(p))==r["sha256"]]
                result["member_results"].append({"path":r["path"],"owner":owner,"baseline_sha256":r["sha256"],"candidate_count":len(candidates),"exact_archive_paths":matches,"status":"BYTE_MATCH" if matches else "NO_BYTE_MATCH","distribution_license_approved":False})
    except Exception as e:
        rec["error"]=str(e)
        for r in members:
            if not any(m["path"]==r["path"] for m in result["member_results"]):
                result["member_results"].append({"path":r["path"],"owner":owner,"status":"NOT_VERIFIED","reason":str(e)})
    finally:
        pathlib.Path(name+".nupkg").unlink(missing_ok=True)
result["member_counts"]=dict(Counter(r["status"] for r in result["member_results"]))
result["package_counts"]=dict(Counter(r["status"] for r in result["packages"]))
result["apphost"]="NOT_VERIFIED; host prefix is not a manifest member"
result["native_license_scope"]={"D3DCompiler_47_cor3.dll":"Windows SDK license; separate approval required","PresentationNative_cor3.dll":".NET Library license","vcruntime140_cor3.dll":".NET Library license","wpfgfx_cor3.dll":".NET Library license"}
save(pathlib.Path("result.json"),json.dumps(result,indent=2,ensure_ascii=False).encode())
print("TUR035 RESULT",json.dumps({"packages":result["package_counts"],"members":result["member_counts"],"production":result["production"]}))
for p in result["packages"]:
    print("TUR035 PACKAGE",json.dumps({k:p.get(k) for k in ("owner","status","catalog_archive_hash_match","signature_verify_exit","build_content_hash_match","nuget_content_sha512","error","license_documents")},ensure_ascii=False))
