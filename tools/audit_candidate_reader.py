"""Audit-only reader. Expected SHA-256 must come from independently trusted evidence.
No legal acceptance, production authorization, arbitrary SQL or update capability."""
import argparse, hashlib, hmac, json, pathlib, re, sqlite3, sys
MAX_BYTES=32*1024*1024
class Denied(Exception): pass
class VerifiedSnapshot(sqlite3.Connection): pass
# Independently verified artifact bytes, not metadata-only permission.
G27_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","TUR058":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200","TUR057":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","TUR056":"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38","TUR055":"91f7369ee251f7897719e4e4bb4cd80ff2af04ef85081294852fa78b60a77e2d","TUR054":"643a3f1b65299dea8bc5d2edb56edc90138b643b0dddb323200e727c1236d2cc","TUR052":"2d5303313a2886918a5506ee516398b745cb42cb102a8d233c12f5cd0e822818","TUR053":"8cb48e72aaba7d25f902618c47edd26ef51e7a7d61182dc0610e8f98a0137b4c"}
M49_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","TUR058":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200","TUR057":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","TUR056":"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38","TUR055":"91f7369ee251f7897719e4e4bb4cd80ff2af04ef85081294852fa78b60a77e2d","TUR054":"643a3f1b65299dea8bc5d2edb56edc90138b643b0dddb323200e727c1236d2cc"}
M28_M29_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","TUR058":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200","TUR057":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","TUR056":"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38"}
M3_M45_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","TUR058":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200"}
M4_M46_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","TUR057":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","TUR058":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200"}
M2_M47_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","TUR059":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884"}
M65_EK2_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","TUR060":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65"}
G123_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","TUR061":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc"}
EK19_EK20_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","TUR062":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8"}
M2863_3_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","TUR063":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06"}
MILITARY_84_85_87_88_PINS={"TUR066":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a"}
def snapshot(path, expected):
    if not re.fullmatch(r"[0-9a-f]{64}",expected): raise Denied("INVALID_EXPECTED_HASH")
    with pathlib.Path(path).open("rb") as f:
        data=f.read(MAX_BYTES+1)
    if not data or len(data)>MAX_BYTES: raise Denied("INVALID_SIZE")
    if not hmac.compare_digest(hashlib.sha256(data).hexdigest(),expected): raise Denied("HASH_MISMATCH")
    if not data.startswith(b"SQLite format 3\x00"): raise Denied("INVALID_FORMAT")
    db=sqlite3.connect(":memory:",factory=VerifiedSnapshot)
    db.verified_sha256=hashlib.sha256(data).hexdigest()
    try:
        db.deserialize(data)
        db.execute("PRAGMA trusted_schema=OFF")
        db.execute("PRAGMA query_only=ON")
        if db.execute("PRAGMA integrity_check").fetchone()!=("ok",): raise Denied("INTEGRITY_FAILED")
        meta=dict(db.execute("SELECT key,value FROM metadata"))
        if meta.get("classification")!="AUDIT_CANDIDATE_NOT_CURRENT_LAW" or meta.get("promotion_allowed")!="false" or meta.get("official_legal_source")!="NOT_VERIFIED" or meta.get("reader_revision")!="TUR049": raise Denied("POLICY_MISMATCH")
        return db
    except BaseException:
        db.close()
        raise
def read_article(db,aid):
    # Known historical boundary defects: only independently verified exact bytes.
    pins={"1632:normal:84":MILITARY_84_85_87_88_PINS,"1632:normal:85":MILITARY_84_85_87_88_PINS,"1632:normal:87":MILITARY_84_85_87_88_PINS,"1632:normal:88":MILITARY_84_85_87_88_PINS,"2863:normal:3":M2863_3_PINS,"2918:ek:19":EK19_EK20_PINS,"2918:ek:20":EK19_EK20_PINS,"2918:geçici:1":G123_PINS,"2918:geçici:2":G123_PINS,"2918:geçici:3":G123_PINS,"2918:normal:65":M65_EK2_PINS,"2918:ek:2":M65_EK2_PINS,"2918:normal:2":M2_M47_PINS,"2918:normal:47":M2_M47_PINS,"2918:normal:3":M3_M45_PINS,"2918:normal:45":M3_M45_PINS,"2918:normal:4":M4_M46_PINS,"2918:normal:46":M4_M46_PINS,"2918:normal:28":M28_M29_PINS,"2918:normal:29":M28_M29_PINS,"2918:normal:49":M49_PINS,"2918:geçici:27":G27_PINS}.get(aid)
    if pins is not None:
        rev=db.execute("SELECT value FROM metadata WHERE key='projection_revision'").fetchone()
        if not rev or not pins.get(rev[0]) or getattr(db,"verified_sha256",None)!=pins[rev[0]]: raise Denied("REVISION_ARTICLE_QUARANTINED")
    row=db.execute("SELECT law,kind,number,status,reason FROM article_index WHERE id=?",(aid,)).fetchone()
    if row is None: raise Denied("UNKNOWN_ARTICLE")
    law,kind,no,status,reason=row
    if status!="CANDIDATE_UNVERIFIED": raise Denied("WITHHELD_REVIEW")
    body=[{"page":p,"ordinal":o,"text":t} for p,o,t in db.execute("SELECT page,ordinal,body FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(aid,))]
    if not body: raise Denied("EMPTY_BODY")
    annotations=[{"type":k,"page":p,"ordinal":o,"text":t,"source_text_sha256":h} for k,p,o,t,h in db.execute("SELECT kind,page,ordinal,raw,source_text_sha256 FROM reader_annotation WHERE article_id=? ORDER BY page,ordinal",(aid,))]
    return {"id":aid,"law":law,"kind":kind,"number":no,"status":status,"classification":"AUDIT_CANDIDATE_NOT_CURRENT_LAW","promotion_allowed":False,"body":body,"annotations":annotations,"annotation_coverage":"SELECTED_SOURCE_ANCHORS_ONLY"}
def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument("--db",required=True);p.add_argument("--expected-sha256",required=True);p.add_argument("--article",required=True)
    a=p.parse_args()
    try:
        db=snapshot(a.db,a.expected_sha256)
        try: result=read_article(db,a.article)
        finally: db.close()
        print(json.dumps(result,ensure_ascii=False))
        return 0
    except Denied as e:
        print(json.dumps({"status":"DENIED","reason":str(e),"promotion_allowed":False}),file=sys.stderr)
        return 65
    except (OSError,sqlite3.Error,ValueError) as e:
        print(json.dumps({"status":"DENIED","reason":type(e).__name__,"promotion_allowed":False}),file=sys.stderr)
        return 65
if __name__=="__main__": sys.exit(main())
