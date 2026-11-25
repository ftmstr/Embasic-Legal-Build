import pathlib as P, sqlite3 as S, hashlib as H, json as J, shutil, subprocess, unicodedata
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes()
    assert sha(b)==h,p
    return b
R=P.Path("tur055-evidence");R.mkdir()
a,b,c=map(root,("prior54","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"643a3f1b65299dea8bc5d2edb56edc90138b643b0dddb323200e727c1236d2cc")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776")]:
    (R/("tur054-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def page(p):return norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
aid="2918:normal:31";holds=["2918:normal:28","2918:normal:29"]
rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid,))]
coords=[(27,o) for o in range(17,38)]+[(28,o) for o in range(39)]+[(29,o) for o in range(18)]
test("78_exact_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in rs))
p26,p27,p28,p29=map(page,(26,27,28,29));start=rs[0]["raw"];end=line(29,18)["raw"]
test("full_pdf_block",p27.count(start)==1 and p29.count(end)==1 and norm(p27[p27.index(start):]+" "+p28+" "+p29[:p29.index(end)])==norm(" ".join(x["raw"] for x in rs)))
test("amendment_and_raw_body",start=="Madde 31 – (Değişik:12/2/2026-7574/6 md.)" and rs[0]["body"]=="(Değişik:12/2/2026-7574/6 md.)" and all(x["body"]==x["raw"] for x in rs[1:]))
rv=J.loads((R/"review-only.json").read_bytes());g=[x for x in rv["list_review"] if x["article"]==aid]
test("two_groups_ten_bents",[(x["lead_page"],x["lead_ordinal"],"".join(y["literal_label"] for y in x["members"])) for x in g]==[(27,18,"abcd"),(28,8,"abcdef")] and all(x["legal_paragraph_number"] is None and x["lead_text"]==line(x["lead_page"],x["lead_ordinal"])["body"] for x in g) and all(y["source_sha256"]==sha(line(y["page"],y["ordinal"])["body"].encode()) for x in g for y in x["members"]))
bad=[(26,26,"İKİNCİ BÖLÜM",holds[0]),(26,27,"Motorlu Araçlara Ait Şartlar",holds[0]),(26,35,"Araçların teknik şartlara uygunluğu:",holds[1])]
test("neighbor_heading_boundary",all(line(p,o)["raw"]==t and t in p26 and s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone()[0]==h for p,o,t,h in bad) and "İKİNCİ BÖLÜM Motorlu Araçlara Ait Şartlar Araçların karayoluna uygunluğu ve teknik esaslar:" in p26 and p27.startswith("Madde 30"))
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
removed=[]
for h,n in zip(holds,(9,7)):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(h,)).fetchone()==("CANDIDATE_UNVERIFIED",)
    rr=d.execute("SELECT * FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(h,)).fetchall();assert len(rr)==n;removed+=rr
    d.execute("UPDATE article_index SET status='WITHHELD_REVIEW',reason='NEW_CHAPTER_OR_TITLE_BOUNDARY_REVIEW' WHERE id=?",(h,));d.execute("DELETE FROM reader_line WHERE article_id=?",(h,))
test("16_quarantine_rows",len(removed)==16 and all((h,2918,p,o,t) in removed for p,o,t,h in bad))
assert d.execute("SELECT status FROM article_index WHERE id=?",(aid,)).fetchone()==("WITHHELD_REVIEW",)
d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid,))
added=[(aid,2918,x["page"],x["ordinal"],x["body"]) for x in rs];d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[]
def annotate(id,k,p,o):
    x=line(p,o);h=sha(x["raw"].encode());d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(id,k,p,o,x["raw"],h));anns.append({"article_id":id,"kind":k,"page":p,"ordinal":o,"raw":x["raw"],"bbox":J.loads(x["bbox"]),"source_text_sha256":h})
for id,p,o in [(aid,27,16),("2918:normal:30",26,35),("2918:normal:32",29,18)]:
    assert d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(id,)).fetchone()==((27,0) if id.endswith(":30") else (p,o+1))
    annotate(id,"TITLE",p,o)
for n,o,oo in [(30,18,[32,33]),(31,22,[34,35,36]),(32,25,[37,38,39])]:
    assert any(z["text"].strip()==str(n) and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(29,o)["spans"]))
    assert all(line(29,j)["classification"]=="FOOTNOTE_CANDIDATE" for j in oo) and norm(" ".join(line(29,j)["raw"] for j in oo)) in p29
    for j in oo:annotate("2918:normal:32","FOOTNOTE",29,j)
test("11_annotations_correct_owner",len(anns)==11 and d.execute("SELECT kind,ordinal FROM reader_annotation WHERE article_id=?",(aid,)).fetchall()==[("TITLE",16)] and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR055' WHERE key='projection_revision'")
test("only_three_status_changes",all(set(x[0] for x in d.execute(q))=={aid,*holds} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("78_added_16_removed",set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and set(d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())==set(removed))
test("35_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==35 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,p,o) for p,o,t,h in bad]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("48_exclusions",len(set(excluded+zones))==48 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
for id,n in [("2918:normal:49",34),("2918:geçici:27",22)]:
    q="SELECT page,ordinal,body FROM {}reader_line WHERE article_id=? ORDER BY page,ordinal"
    test(id+"_unchanged",len(d.execute(q.format(""),(id,)).fetchall())==n and d.execute(q.format(""),(id,)).fetchall()==d.execute(q.format("old."),(id,)).fetchall())
test("prior_annotations_preserved",not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall())
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"article":aid,"body_coordinates":[ev(x) for x in rs],"groups":g,"new_holds":holds,"removed_reader_rows":removed,"excluded_coordinates":[[p,o] for p,o,t,h in bad],"annotations":anns,"legal_acceptance":False},ensure_ascii=False,indent=2))
out={"status":"TUR055_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1473,"withheld_articles":35,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":aid,"new_holds":holds}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2));print(J.dumps(out))
