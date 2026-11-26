import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata,re
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
R=P.Path("tur058-evidence");R.mkdir()
a,b,c=map(root,("prior57","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776"),("tur054-boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51"),("tur055-boundary-review.json","402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372"),("tur056-boundary-review.json","927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c")]:
    (R/("tur057-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in [2,3,4,5,6,7,37]}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
def aid(n):return f"2918:normal:{n}"
added=[];oldrows=[];evidence=[];terms=[];amendments=[]
for n,coords,bad in [(3,[(2,o) for o in range(5,38)]+[(p,o) for p in range(3,7) for o in range(39)]+[(7,o) for o in range(21)],[(7,21),(7,22)]),(45,[(37,o) for o in range(5,12)],[(37,o) for o in range(12,16)])]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid(n),))]
    test(f"m{n}_exact_membership_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords+bad)
    good=rs[:len(coords)];start=good[0]["raw"];stop=line(*bad[0])["raw"]
    pp=sorted({p for p,o in coords});assert pages[pp[0]].count(start)==1 and pages[pp[-1]].count(stop)==1
    if n==3:
        block=pages[2][pages[2].index(start):]+" "+" ".join(pages[p] for p in range(3,7))+" "+pages[7][:pages[7].index(stop)]
    else:block=pages[37][pages[37].index(start):pages[37].index(stop)]
    test(f"m{n}_full_independent_pdf_block",norm(block)==norm(" ".join(x["raw"] for x in good)))
    test(f"m{n}_only_header_removed",all(x["body"]==(x["raw"].split(" – ",1)[1] if i==0 else x["raw"]) and x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for i,x in enumerate(good)))
    oldrows.extend([aid(n),2918,x["page"],x["ordinal"],x["body"]] for x in rs)
    added.extend((aid(n),2918,x["page"],x["ordinal"],x["body"]) for x in good)
    evidence.extend({"article_id":aid(n),**ev(x)} for x in good)
    amendments.extend({"article_id":aid(n),**ev(x)} for x in good if re.search(r"\((Ek|Değişik|Mülga)",x["raw"]))
    if n==3:
        for x in good[1:]:
            if J.loads(x["bbox"])[0]>100 and re.match(r"^[^:]{1,65}:",x["raw"]):
                term=x["raw"].split(":",1)[0]
                if x["raw"].startswith("Taşıt yolu: (Kaplama):"):term="Taşıt yolu: (Kaplama)"
                terms.append({"label":term,"legal_paragraph_number":None,"status":"SOURCE_DEFINITION_CANDIDATE",**ev(x)})
test("prior_223_rows_exact",oldrows==J.loads((a/"boundary-review.json").read_bytes())["removed_reader_rows"])
test("90_distinct_definition_anchors",len(terms)==90 and len({x["label"] for x in terms})==90 and terms[0]["label"]=="Trafik" and terms[-1]["label"]=="Bisiklet şeridi" and any(x["label"]=="Minibüs" and "Mülga" in x["raw"] for x in terms))
test("amendments_retained",len(amendments)==21 and any(x["raw"]=="Madde 45 – (Değişik: 12/7/2013-6495/19 md.)" for x in amendments) and any(x["page"]==7 and x["ordinal"]==18 for x in amendments))
test("new_m2_chapter_boundary",pages[2].startswith("üzerinde kamu hizmeti gören araçların, karayolu araçlarına ayrılan kısımlarında da, Bu Kanun hükümleri uygulanır. İKİNCİ BÖLÜM Tanımlar Tanımlar: Madde 3") and all(s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=2 AND ordinal=?",(o,)).fetchone()[0]==aid(2) for o in (2,3)))
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
assert d.execute("SELECT status FROM article_index WHERE id=?",(aid(2),)).fetchone()==("CANDIDATE_UNVERIFIED",)
removed=d.execute("SELECT * FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(aid(2),)).fetchall()
test("13_m2_rows_quarantined",len(removed)==13 and removed[-2:]==[(aid(2),2918,2,2,"İKİNCİ BÖLÜM"),(aid(2),2918,2,3,"Tanımlar")])
d.execute("UPDATE article_index SET status='WITHHELD_REVIEW',reason='NEW_CHAPTER_BOUNDARY_REVIEW' WHERE id=?",(aid(2),));d.execute("DELETE FROM reader_line WHERE article_id=?",(aid(2),))
for n in (3,45):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid(n),)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid(n),))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
title=line(2,4);assert title["raw"]=="Tanımlar:" and title["classification"]=="HEADING_CANDIDATE"
d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid(3),"TITLE",2,4,title["raw"],sha(title["raw"].encode())))
test("m45_note40_correct_owner_preserved",d.execute("SELECT kind,page,ordinal FROM reader_annotation WHERE article_id=? ORDER BY page,ordinal",(aid(45),)).fetchall()==[("TITLE",37,4)]+[("FOOTNOTE",37,o) for o in range(34,37)] and any(z["text"].strip()=="40" and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(37,4)["spans"])) and norm(" ".join(line(37,o)["raw"] for o in range(34,37))) in pages[37])
test("one_new_title_no_overlap",d.execute("SELECT count(*) FROM reader_annotation").fetchone()[0]==56 and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR058' WHERE key='projection_revision'")
test("only_three_status_changes",all(set(x[0] for x in d.execute(q))=={aid(n) for n in (2,3,45)} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("217_added_13_removed",len(added)==217 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and set(d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())==set(removed))
test("32_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==32 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("56_exclusions",len(set(excluded+zones))==56 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
for id,n in [(aid(4),22),(aid(46),74),(aid(28),7),(aid(29),6),(aid(49),34),("2918:geçici:27",22),(aid(31),78)]:
    q="SELECT page,ordinal,body FROM {}reader_line WHERE article_id=? ORDER BY page,ordinal"
    test(id+"_unchanged",len(d.execute(q.format(""),(id,)).fetchall())==n and d.execute(q.format(""),(id,)).fetchall()==d.execute(q.format("old."),(id,)).fetchall())
rv=J.loads((R/"review-only.json").read_bytes())
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"definition_anchors":terms,"amendment_anchors":amendments,"title":ev(title),"new_hold":aid(2),"removed_reader_rows":removed,"excluded_coordinates":[[2,2],[2,3]],"legal_acceptance":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR058_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1476,"withheld_articles":32,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[aid(3),aid(45)],"new_hold":aid(2)}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
