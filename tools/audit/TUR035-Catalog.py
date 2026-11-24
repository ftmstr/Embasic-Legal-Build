"""TUR035 catalog-only follow-up: reuse existing nine signed-package and 385 parity observations."""
import gzip, hashlib, json, pathlib, urllib.request
from collections import Counter
from urllib.parse import urlsplit
root=pathlib.Path("tur035-catalog-evidence"); root.mkdir(exist_ok=True)
def sha(b): return hashlib.sha256(b).hexdigest()
def official(url):
    p=urlsplit(url)
    if p.scheme!="https" or p.netloc!="api.nuget.org": raise ValueError("unexpected endpoint")
    with urllib.request.urlopen(url,timeout=60) as r:
        p=urlsplit(r.url)
        if p.scheme!="https" or p.netloc!="api.nuget.org": raise ValueError("unexpected redirect")
        b=r.read()
    if b.startswith(b"\x1f\x8b"): b=gzip.decompress(b)
    return b,json.loads(b)
files=list(pathlib.Path("prior").rglob("result.json"))
assert len(files)==1
b=files[0].read_bytes()
assert sha(b)=="b431a62875fd87ea52ec5f09709198721882481e80f0a093c9534258896f06d1"
d=json.loads(b)
assert d["original_head"]=="9f9af1fb33fb0f69c6d3f1f408e467a856e4ca0f"
assert d["member_counts"]=={"BYTE_MATCH":385}
assert len(d["packages"])==9
result={"previous_artifact_id":14934130303,"previous_result_sha256":sha(b),"original_head":d["original_head"],"original_exe_sha256":d["original_exe_sha256"],"production":"NO-GO","license_release":"NO-GO","packages":[]}
for p in d["packages"]:
    id=p["id"].lower(); version=p["version"]; name=id+"."+version
    rec={"id":p["id"],"version":version,"prior_archive_sha512_base64":p["archive_sha512_base64"],"prior_nuget_content_sha512":p["nuget_content_sha512"],"prior_archive_bytes":p["bytes"],"status":"NOT_VERIFIED"}
    result["packages"].append(rec)
    try:
        leaf_url=f"https://api.nuget.org/v3/registration5-gz-semver2/{id}/{version}.json"
        leaf_bytes,leaf=official(leaf_url)
        entry=leaf["catalogEntry"]; cat_url=entry if isinstance(entry,str) else entry["@id"]
        cat_bytes,cat=official(cat_url)
        assert cat["id"].lower()==id and cat["version"].lower()==version.lower()
        for label,raw,url in [("registration",leaf_bytes,leaf_url),("catalog",cat_bytes,cat_url)]:
            path=f"{name}.{label}.json"; (root/path).write_bytes(raw)
            rec[label]={"path":path,"sha256":sha(raw),"url":url,"bytes":len(raw)}
        rec.update(catalog_algorithm=cat.get("packageHashAlgorithm"),catalog_sha512=cat.get("packageHash"),catalog_bytes=cat.get("packageSize"))
        algorithm=cat.get("packageHashAlgorithm","").upper()
        rec["catalog_archive_hash_match"]=algorithm=="SHA512" and cat.get("packageHash")==p["archive_sha512_base64"]
        rec["catalog_content_hash_match"]=algorithm=="SHA512" and cat.get("packageHash")==p["nuget_content_sha512"]
        rec["catalog_size_match"]=cat.get("packageSize")==p["bytes"]
        rec["status"]="CATALOG_ARCHIVE_MATCH" if rec["catalog_archive_hash_match"] and rec["catalog_size_match"] else "CATALOG_NOT_ARCHIVE_MATCH"
    except Exception as e: rec["error"]=str(e)
result["counts"]=dict(Counter(p["status"] for p in result["packages"]))
(root/"catalog-result.json").write_text(json.dumps(result,indent=2),encoding="utf-8")
print("TUR035 CATALOG",json.dumps(result["counts"]))
for p in result["packages"]:
    print("TUR035 CATALOG PACKAGE",json.dumps({k:p.get(k) for k in ("id","version","status","catalog_archive_hash_match","catalog_content_hash_match","catalog_size_match","error")}))
