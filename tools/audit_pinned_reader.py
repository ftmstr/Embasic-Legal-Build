"""TUR050 audit-only integration boundary. Trust this exact reviewed Git source
independently of the supplied DB. Pins are not signatures or legal approval."""
import argparse,json,sys,sqlite3
from audit_candidate_reader import snapshot,read_article,Denied
PINS={
 "TUR052":{"sha256":"2d5303313a2886918a5506ee516398b745cb42cb102a8d233c12f5cd0e822818","artifact":15060085952,"run":45693327586,"head":"444ebcc0f6104f6a1fa9481c37c18f0a440b7a5e"},
 "TUR051":{"sha256":"6e8d9b2b12295cb5ebfeca98d3bf6921955a2d6e123b2567cd183b96b7fb5ab5","artifact":15058496001,"run":45689770047,"head":"f3b1766579d4e120bf1cbb3e3da15fe2379c13b1"},
 "TUR049":{"sha256":"8cf36bf2937f7c73020d9bd6975ec9a2d75b84d0d45d42fe99a0582758f18919","artifact":15057816038,"run":45687953803,"head":"bfd55c2326dba752a8cc8ae54bbed8b7049f356d"},
 "TUR050":{"sha256":"a884a9e4b26977c75b1baec3dddefde4ade3426719d3f9b0e44c82d2c87eb1a9","artifact":15056664563,"run":45688754638,"head":"a7982a034e24daec6b2d07713086be2b4903cf6a"}
}
def main():
    p=argparse.ArgumentParser(description=__doc__,allow_abbrev=False)
    p.add_argument("--db",required=True);p.add_argument("--revision",required=True,choices=tuple(PINS));p.add_argument("--article",required=True)
    a=p.parse_args();pin=PINS[a.revision]
    try:
        db=snapshot(a.db,pin["sha256"])
        try:
            meta=dict(db.execute("SELECT key,value FROM metadata"))
            if meta.get("projection_revision","TUR049")!=a.revision:raise Denied("REVISION_MISMATCH")
            v=read_article(db,a.article)
        finally:db.close()
        v["evidence"]={"revision":a.revision,**pin}
        print(json.dumps(v,ensure_ascii=False));return 0
    except (Denied,OSError,sqlite3.Error,ValueError) as e:
        print(json.dumps({"status":"DENIED","reason":str(e) if isinstance(e,Denied) else type(e).__name__,"promotion_allowed":False}),file=sys.stderr);return 65
if __name__=="__main__":sys.exit(main())
