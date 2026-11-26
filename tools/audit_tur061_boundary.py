import pathlib as P, sqlite3 as S, hashlib as H, json as J, shutil, subprocess, unicodedata
def sha(b): return H.sha256(b).hexdigest()
def pin(p,h):
    b=p.read_bytes(); assert sha(b)==h,p; return b
def root(n): return next(P.Path(n).rglob("result.json")).parent
R=P.Path("tur061-evidence"); R.mkdir()
a,b,c=map(root,("prior60","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
hashes={"review-only.json":"fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747","boundary-review.json":"8e55d10f5704ec6d1959885edf20b20d165531a86fb64eca32141989a80485b8","tur052-boundary-review.json":"49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474","tur053-boundary-review.json":"149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776","tur054-boundary-review.json":"3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51","tur055-boundary-review.json":"402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372","tur056-boundary-review.json":"927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c","tur057-boundary-review.json":"360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec","tur058-boundary-review.json":"9cbb679c51a53b061ae4515b4f70799181185f7af73489c69e756e92f7f923b5","tur059-boundary-review.json":"d0c4388cd5d2f5cdc60ce9ab0e6cf4cd146e5ed2051e71bdd780140555ad7d61"}
for n,h in hashes.items(): (R/("tur060-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t): return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (92,93,94)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def g(n):return f"2918:geçici:{n}"
def e(n):return f"2918:ek:{n}"
added=[];evidence=[]
for n,coords,end in [(1,[(93,o) for o in range(9,19)],(93,19)),(2,[(93,o) for o in range(19,27)],(93,27)),(3,[(93,o) for o in range(27,35)]+[(94,o) for o in range(5)],(94,5))]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(g(n),))]
    test(f"g{n}_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in rs))
    start=rs[0]["raw"];stop=line(*end)["raw"]
    assert pages[93].count(start)==1 and pages[end[0]].count(stop)==1
    block=pages[93][pages[93].index(start):pages[93].index(stop)] if end[0]==93 else pages[93][pages[93].index(start):]+" "+pages[94][:pages[94].index(stop)]
    test(f"g{n}_independent_full_pdf_block",norm(block)==norm(" ".join(x["raw"] for x in rs)))
    test(f"g{n}_only_header_removed",all(x["body"]==(x["raw"].split(" – ",1)[1] if i==0 else x["raw"]) for i,x in enumerate(rs)))
    added.extend((g(n),2918,x["page"],x["ordinal"],x["body"]) for x in rs)
    evidence.extend({"article_id":g(n),**ev(x)} for x in rs)
test("g2_two_literal_bents",line(93,20)["raw"].endswith("kadar;") and line(93,21)["raw"].startswith("a) ") and line(93,23)["raw"].startswith("b) ") and line(93,26)["raw"]=="Kullanabilirler.")
test("separate_chapter_and_header",norm(" ".join(line(93,o)["raw"] for o in range(7,10))) in pages[93] and line(93,7)["raw"]=="ONİKİNCİ KISIM" and line(93,8)["raw"]=="Geçici Hükümler")
anchor=line(92,18);note=[line(92,o) for o in range(34,37)]
test("note90_exact_superscript_owner",anchor["raw"]==anchor["body"]+"90" and any(z["text"].strip()=="90" and z["flags"]&1 and z["size"]<=9 for z in J.loads(anchor["spans"])) and s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=92 AND ordinal=18").fetchone()[0]==e(18) and note[0]["raw"].startswith("90 ") and norm(" ".join(x["raw"] for x in note)) in pages[92])
bad=[(92,o,e(19)) for o in range(34,37)]+[(93,7,e(20))]
test("four_misassigned_memberships",all(s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone()[0]==aid for p,o,aid in bad))
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
removed=[]
for aid in (e(19),e(20)):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid,)).fetchone()==("CANDIDATE_UNVERIFIED",)
    rr=d.execute("SELECT * FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(aid,)).fetchall();assert len(rr)==6;removed.extend(rr)
    d.execute("UPDATE article_index SET status='WITHHELD_REVIEW',reason='NEW_FOOTNOTE_OR_CHAPTER_BOUNDARY_REVIEW' WHERE id=?",(aid,));d.execute("DELETE FROM reader_line WHERE article_id=?",(aid,))
test("twelve_rows_quarantined",len(removed)==12 and all((aid,2918,p,o,line(p,o)["body"]) in removed for p,o,aid in bad))
for n in (1,2,3):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(g(n),)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(g(n),))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[]
for k,p,o in [("TITLE",92,9)]+[("FOOTNOTE",92,o) for o in range(34,37)]:
    x=line(p,o);d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(e(18),k,p,o,x["raw"],sha(x["raw"].encode())));anns.append({"article_id":e(18),"kind":k,**ev(x)})
test("four_annotations_correct_owner",line(92,9)["raw"]=="Noterliklere devir:" and len(anns)==4 and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR061' WHERE key='projection_revision'")
test("only_five_status_changes",all(set(x[0] for x in d.execute(q))=={g(1),g(2),g(3),e(19),e(20)} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("31_added_12_removed",len(added)==31 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and set(d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())==set(removed))
test("27_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==27 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]+[(2918,p,o) for p,o,aid in bad]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("60_exclusions",len(set(excluded+zones))==60 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones) and not d.execute("SELECT 1 FROM reader_line WHERE law=2918 AND page=93 AND ordinal=8").fetchall())
rv=J.loads((R/"review-only.json").read_bytes())
test("grammar_definition_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and len(J.loads((R/"tur058-boundary-review.json").read_bytes())["definition_anchors"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
test("distinct_normal_ek_temporary_ids",d.execute("SELECT kind,number FROM article_index WHERE id IN (?,?,?) ORDER BY kind",("2918:normal:2",e(2),g(2))).fetchall()==[("ek","2"),("geçici","2"),("normal","2")])
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
chapter=[ev(line(93,o)) for o in (7,8)]
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"literal_group":{"article_id":g(2),"lead":[93,20],"members":[[93,21],[93,23]],"legal_paragraph_number":None},"chapter":chapter,"annotations":anns,"note90":{"owner":e(18),"anchor":ev(anchor),"source_rows":[ev(x) for x in note]},"new_holds":[e(19),e(20)],"removed_reader_rows":removed,"excluded_coordinates":[[p,o] for p,o,aid in bad],"legal_acceptance":False,"automatic_effect":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR061_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1481,"withheld_articles":27,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[g(1),g(2),g(3)],"new_holds":[e(19),e(20)]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
