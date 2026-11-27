"""TUR050 audit-only integration boundary. Trust this exact reviewed Git source
independently of the supplied DB. Pins are not signatures or legal approval."""
import argparse,json,sys,sqlite3
from audit_candidate_reader import snapshot,read_article,Denied
PINS={
 "TUR062":{"sha256":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","artifact":15191499573,"run":45987493698,"head":"ba600ca1ecac739b17afd9b0c8d2cae27a93e289"},
 "TUR061":{"sha256":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","artifact":15156381649,"run":45901669617,"head":"6c211dcde4b1d77b4352aac88059112d356086dc"},
 "TUR060":{"sha256":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","artifact":15144913108,"run":45876381548,"head":"66b74ca3702c673f4fac3e5a3831936768474185"},
 "TUR059":{"sha256":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","artifact":15137949379,"run":45862843182,"head":"e1bbe15a26e88917a9df4fe278f8f9b765a47ece"},
 "TUR058":{"sha256":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200","artifact":15124392385,"run":45834946840,"head":"2c5d714297cd7815b16bc03e777c9d7f28c8fb30"},
 "TUR057":{"sha256":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","artifact":15093895209,"run":45769614123,"head":"07fcb280f84741b05afe63ae15124a0d2530bd5d"},
 "TUR056":{"sha256":"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38","artifact":15093086567,"run":45768293248,"head":"69e9c860fe5b5dfe94c85f4b0c9040ca2a199444"},
 "TUR055":{"sha256":"91f7369ee251f7897719e4e4bb4cd80ff2af04ef85081294852fa78b60a77e2d","artifact":15064090183,"run":45700585363,"head":"523e7e5e602669d171d7f869753df894eeb3d6ec"},
 "TUR054":{"sha256":"643a3f1b65299dea8bc5d2edb56edc90138b643b0dddb323200e727c1236d2cc","artifact":15060707849,"run":45696360940,"head":"f942494456b3f63c91e8e7937375628be7700bec"},
 "TUR053":{"sha256":"8cb48e72aaba7d25f902618c47edd26ef51e7a7d61182dc0610e8f98a0137b4c","artifact":15060641088,"run":45694684288,"head":"9954c422be0810d4534c7055a707cb758ca8a932"},
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
