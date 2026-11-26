import pathlib as P, sqlite3 as S, hashlib as H, json as J, shutil, subprocess, unicodedata
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
R=P.Path("tur056-evidence");R.mkdir()
a,b,c=map(root,("prior55","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"91f7369ee251f7897719e4e4bb4cd80ff2af04ef85081294852fa78b60a77e2d")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776"),("tur054-boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51")]:
    (R/("tur055-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
page=norm(subprocess.check_output(["pdftotext","-f","26","-l","26","-layout",str(pdf),"-"]).decode())
def line(o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=26 AND ordinal=?",(o,)).fetchone())
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
ids=["2918:normal:28","2918:normal:29"]
added=[];original=[];evidence=[]
for aid,start,end,stop in [(ids[0],19,25,26),(ids[1],29,34,35)]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid,))]
    good=[x for x in rs if x["ordinal"]<=end]
    test(aid+"_exact_coordinates",[(x["page"],x["ordinal"]) for x in good]==[(26,o) for o in range(start,end+1)] and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in good))
    fst,last=good[0]["raw"],line(stop)["raw"]
    test(aid+"_full_independent_pdf_block",page.count(fst)==1 and page.index(fst)<page.index(last) and norm(page[page.index(fst):page.index(last)])==norm(" ".join(x["raw"] for x in good)))
    test(aid+"_raw_body_transform",all(x["body"]==(x["raw"].split(" – ",1)[1] if x["ordinal"]==start else x["raw"][:-2] if x["ordinal"]==21 else x["raw"]) for x in good))
    added.extend((aid,2918,26,x["ordinal"],x["body"]) for x in good)
    original.extend([aid,2918,26,x["ordinal"],x["body"]] for x in rs)
    evidence.extend({"article_id":aid,"page":26,"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())} for x in good)
test("prior_16_rows_match",original==J.loads((a/"boundary-review.json").read_bytes())["removed_reader_rows"])
test("chapter_and_next_title_separate","İKİNCİ BÖLÜM Motorlu Araçlara Ait Şartlar Araçların karayoluna uygunluğu ve teknik esaslar:" in page and line(35)["raw"]=="Araçların teknik şartlara uygunluğu:")
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
for aid in ids:
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid,)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid,))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[]
def annotate(aid,k,o):
    x=line(o);h=sha(x["raw"].encode());d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid,k,26,o,x["raw"],h))
    anns.append({"article_id":aid,"kind":k,"page":26,"ordinal":o,"raw":x["raw"],"bbox":J.loads(x["bbox"]),"source_text_sha256":h})
for n,o in [(27,12),(28,18),(29,28)]:
    aid=f"2918:normal:{n}";assert line(o)["classification"]=="HEADING_CANDIDATE" and d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(aid,)).fetchone()==(26,o+1)
    annotate(aid,"TITLE",o)
for n,anchor,oo,owner in [(28,15,range(36,39),27),(29,21,range(39,43),28)]:
    aid=f"2918:normal:{owner}";x=line(anchor)
    test(f"note_{n}_owner",any(z["text"].strip()==str(n) and z["flags"]&1 and z["size"]<=9 for z in J.loads(x["spans"])) and x["raw"]==x["body"]+str(n) and s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=26 AND ordinal=?",(anchor,)).fetchone()[0]==aid and line(oo.start)["raw"].startswith(str(n)+" ") and all(line(o)["classification"]=="FOOTNOTE_CANDIDATE" for o in oo) and norm(" ".join(line(o)["raw"] for o in oo)) in page)
    for o in oo:annotate(aid,"FOOTNOTE",o)
test("ten_new_annotations_separate",len(anns)==10 and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall() and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall())
d.execute("UPDATE metadata SET value='TUR056' WHERE key='projection_revision'")
test("only_two_status_changes",all(set(x[0] for x in d.execute(q))==set(ids) for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("13_added_no_removed",len(added)==13 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and not d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())
test("33_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==33 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("48_exclusions",len(set(excluded+zones))==48 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
for aid,n in [("2918:normal:49",34),("2918:geçici:27",22),("2918:normal:31",78)]:
    q="SELECT page,ordinal,body FROM {}reader_line WHERE article_id=? ORDER BY page,ordinal"
    test(aid+"_unchanged",len(d.execute(q.format(""),(aid,)).fetchall())==n and d.execute(q.format(""),(aid,)).fetchall()==d.execute(q.format("old."),(aid,)).fetchall())
rv=J.loads((R/"review-only.json").read_bytes())
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"chapter_coordinates":[[26,26],[26,27]],"next_title_coordinate":[26,35],"annotations":anns,"note_anchors":[{"note":28,"article_id":"2918:normal:27","page":26,"ordinal":15},{"note":29,"article_id":"2918:normal:28","page":26,"ordinal":21}],"legal_acceptance":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR056_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1475,"withheld_articles":33,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":ids}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
