import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
R=P.Path("tur057-evidence");R.mkdir()
a,b,c=map(root,("prior56","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776"),("tur054-boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51"),("tur055-boundary-review.json","402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372")]:
    (R/("tur056-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (7,8,37,38,39)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
def aid(n):return f"2918:normal:{n}"
rv=J.loads((R/"review-only.json").read_bytes())
added=[];evidence=[];groups=[]
for n,pp,coords,stop,spec in [(4,[7,8],[(7,o) for o in range(24,36)]+[(8,o) for o in range(10)],10,[(7,26,"ab"),(8,5,"ab")]),(46,[37,38,39],[(37,o) for o in range(17,34)]+[(38,o) for o in range(37)]+[(39,o) for o in range(20)],20,[(37,18,"abcdefghijklmn"),(38,24,"abcdef")])]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid(n),))]
    test(f"m{n}_exact_body_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in rs))
    texts=[pages[p] for p in pp];start=rs[0]["raw"];end=line(pp[-1],stop)["raw"]
    assert texts[0].count(start)==1 and texts[-1].count(end)==1
    texts[0]=texts[0][texts[0].index(start):];texts[-1]=texts[-1][:texts[-1].index(end)]
    for j,p in enumerate(pp[:-1]):
        note=norm(" ".join(x[0] for x in s.execute("SELECT raw FROM line WHERE law=2918 AND page=? AND classification='FOOTNOTE_CANDIDATE' ORDER BY ordinal",(p,))))
        assert note and texts[j].count(note)==1
        texts[j]=texts[j].replace(note,"")
    test(f"m{n}_complete_independent_pdf_block",norm(" ".join(texts))==norm(" ".join(x["raw"] for x in rs)))
    test(f"m{n}_only_header_and_confirmed_superscript_removed",all(x["body"]==(x["raw"].split(" – ",1)[1] if i==0 else x["raw"][:-2] if (x["page"],x["ordinal"])==(38,18) else x["raw"]) for i,x in enumerate(rs)))
    g=[x for x in rv["list_review"] if x["article"]==aid(n)]
    test(f"m{n}_literal_groups_anchors",[(x["lead_page"],x["lead_ordinal"],"".join(y["literal_label"] for y in x["members"])) for x in g]==spec and all(x["legal_paragraph_number"] is None and x["lead_text"]==line(x["lead_page"],x["lead_ordinal"])["body"] for x in g) and all(y["source_sha256"]==sha(line(y["page"],y["ordinal"])["body"].encode()) for x in g for y in x["members"]))
    groups.extend(g)
    added.extend((aid(n),2918,x["page"],x["ordinal"],x["body"]) for x in rs)
    evidence.extend({"article_id":aid(n),**ev(x)} for x in rs)
test("m4_four_literal_numbered_items",line(7,34)["raw"]=="Kurulun görev ve yetkileri şunlardır;" and all(line(p,o)["raw"].startswith(f"{i}. ") for i,(p,o) in enumerate([(7,35),(8,1),(8,2),(8,3)],1)))
test("m46_three_numeric_children",line(37,25)["raw"].startswith("e) ") and all(line(37,o)["raw"].startswith(f"{i}) ") for i,o in enumerate((27,28,30),1)))
bad=[(7,21,3),(7,22,3)]+[(37,o,45) for o in range(12,16)]
test("six_adjacent_chapter_leaks",all(s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone()[0]==aid(n) for p,o,n in bad) and norm(" ".join(line(7,o)["raw"] for o in range(21,25))) in pages[7] and norm(" ".join(line(37,o)["raw"] for o in range(12,18))) in pages[37])
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
removed=[]
for n,count in [(3,212),(45,11)]:
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid(n),)).fetchone()==("CANDIDATE_UNVERIFIED",)
    rr=d.execute("SELECT * FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(aid(n),)).fetchall();assert len(rr)==count
    removed.extend(rr);d.execute("UPDATE article_index SET status='WITHHELD_REVIEW',reason='NEW_CHAPTER_BOUNDARY_REVIEW' WHERE id=?",(aid(n),));d.execute("DELETE FROM reader_line WHERE article_id=?",(aid(n),))
test("223_rows_quarantined",len(removed)==223 and all((aid(n),2918,p,o,line(p,o)["body"]) in removed for p,o,n in bad))
for n in (4,46):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid(n),)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid(n),))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[];notes=[]
def annotate(n,k,p,o):
    x=line(p,o);h=sha(x["raw"].encode());d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid(n),k,p,o,x["raw"],h));anns.append({"article_id":aid(n),"kind":k,**ev(x)})
for n,p,o in [(4,7,23),(5,8,10),(45,37,4),(46,37,16),(47,39,20)]:
    assert line(p,o)["classification"]=="HEADING_CANDIDATE" and d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(aid(n),)).fetchone()==(p,o+1)
    annotate(n,"TITLE",p,o)
for n,p,num,anchors,oo in [(4,7,4,[22,23],range(36,38)),(5,8,5,[10],range(36,39)),(45,37,40,[4],range(34,37)),(46,38,41,[18],range(37,39)),(47,39,42,[20],range(37,39))]:
    test(f"note_{num}_source_anchors",all(any(z["text"].strip()==str(num) and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(p,o)["spans"])) and line(p,o)["raw"]==line(p,o)["body"]+str(num) for o in anchors) and line(p,oo.start)["raw"].startswith(str(num)+" ") and all(line(p,o)["classification"]=="FOOTNOTE_CANDIDATE" for o in oo) and norm(" ".join(line(p,o)["raw"] for o in oo)) in pages[p])
    for o in oo:annotate(n,"FOOTNOTE",p,o)
    notes.append({"note":num,"article_id":aid(n),"anchors":[ev(line(p,o)) for o in anchors],"shared_chapter_anchor":num==4,"note_coordinates":[[p,o] for o in oo]})
test("17_annotations_correct_separate",len(anns)==17 and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall() and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall())
d.execute("UPDATE metadata SET value='TUR057' WHERE key='projection_revision'")
test("only_four_status_changes",all(set(x[0] for x in d.execute(q))=={aid(n) for n in (3,4,45,46)} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("96_added_223_held",len(added)==96 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and set(d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())==set(removed))
test("33_held_no_body_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==33 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,p,o) for p,o,n in bad]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("54_exclusions",len(set(excluded+zones))==54 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
for id,n in [(aid(28),7),(aid(29),6),(aid(49),34),("2918:geçici:27",22),(aid(31),78)]:
    q="SELECT page,ordinal,body FROM {}reader_line WHERE article_id=? ORDER BY page,ordinal"
    test(id+"_unchanged",len(d.execute(q.format(""),(id,)).fetchall())==n and d.execute(q.format(""),(id,)).fetchall()==d.execute(q.format("old."),(id,)).fetchall())
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"groups":groups,"numeric_lists":[{"article_id":aid(4),"lead":[7,34],"items":[[7,35],[8,1],[8,2],[8,3]],"legal_paragraph_number":None},{"article_id":aid(46),"parent":[37,25],"items":[[37,27],[37,28],[37,30]],"legal_paragraph_number":None}],"annotations":anns,"notes":notes,"new_holds":[aid(3),aid(45)],"removed_reader_rows":removed,"excluded_coordinates":[[p,o] for p,o,n in bad],"legal_acceptance":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR057_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1475,"withheld_articles":33,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[aid(4),aid(46)],"new_holds":[aid(3),aid(45)]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
