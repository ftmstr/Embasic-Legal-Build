"""Audit-only reader. Expected SHA-256 must come from independently trusted evidence.
No legal acceptance, production authorization, arbitrary SQL or update capability."""
import argparse, hashlib, hmac, json, pathlib, re, sqlite3, sys
MAX_BYTES=32*1024*1024
class Denied(Exception): pass
class VerifiedSnapshot(sqlite3.Connection): pass
# Independently verified artifact bytes, not metadata-only permission.
G27_PINS={"TUR052":"2d5303313a2886918a5506ee516398b745cb42cb102a8d233c12f5cd0e822818","TUR053":"8cb48e72aaba7d25f902618c47edd26ef51e7a7d61182dc0610e8f98a0137b4c"}
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
    if aid=="2918:normal:49": raise Denied("REVISION_ARTICLE_QUARANTINED")
    # Known historical boundary defect: deny by default until a reviewed revision.
    if aid=="2918:geçici:27":
        rev=db.execute("SELECT value FROM metadata WHERE key='projection_revision'").fetchone()
        if not rev or not G27_PINS.get(rev[0]) or getattr(db,"verified_sha256",None)!=G27_PINS[rev[0]]: raise Denied("REVISION_ARTICLE_QUARANTINED")
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
