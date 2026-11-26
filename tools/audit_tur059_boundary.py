import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
R=P.Path("tur059-evidence");R.mkdir()
a,b,c=map(root,("prior58","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","9cbb679c51a53b061ae4515b4f70799181185f7af73489c69e756e92f7f923b5"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776"),("tur054-boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51"),("tur055-boundary-review.json","402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372"),("tur056-boundary-review.json","927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c"),("tur057-boundary-review.json","360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec")]:
    (R/("tur058-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (1,2,39,40,41)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
def aid(n):return f"2918:normal:{n}"
def note(p,oo):return norm(" ".join(line(p,o)["raw"] for o in oo))
def anchor(p,o,n):
    return any(z["text"].strip()==str(n) and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(p,o)["spans"]))
rv=J.loads((R/"review-only.json").read_bytes())
added=[];evidence=[]
for n,p,q,coords,bad,stop,no in [(2,1,2,[(1,o) for o in range(19,28)]+[(2,o) for o in range(2)],[(2,2),(2,3)],2,range(28,43)),(47,39,40,[(39,o) for o in range(21,37)]+[(40,o) for o in range(32)],[],32,range(37,39))]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid(n),))]
    test(f"m{n}_exact_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords+bad)
    good=rs[:len(coords)];start=good[0]["raw"];end=line(q,stop)["raw"];ft=note(p,no)
    assert pages[p].count(start)==1 and pages[q].count(end)==1 and pages[p].count(ft)==1
    test(f"m{n}_complete_independent_pdf_block",norm(pages[p][pages[p].index(start):].replace(ft,"")+" "+pages[q][:pages[q].index(end)])==norm(" ".join(x["raw"] for x in good)))
    test(f"m{n}_body_transform_only_header",all(x["body"]==(x["raw"].split(" – ",1)[1] if i==0 else x["raw"]) and x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for i,x in enumerate(good)))
    if n==2:
        test("m2_prior_13_quarantine_rows",[[aid(n),2918,x["page"],x["ordinal"],x["body"]] for x in rs]==J.loads((a/"boundary-review.json").read_bytes())["removed_reader_rows"])
    added.extend((aid(n),2918,x["page"],x["ordinal"],x["body"]) for x in good)
    evidence.extend({"article_id":aid(n),**ev(x)} for x in good)
g=[x for x in rv["list_review"] if x["article"]==aid(47)]
test("m47_two_groups_nine_bents",[(x["lead_page"],x["lead_ordinal"],"".join(y["literal_label"] for y in x["members"])) for x in g]==[(39,21,"abcd"),(39,35,"abcde")] and all(x["legal_paragraph_number"] is None and x["lead_text"]==line(x["lead_page"],x["lead_ordinal"])["body"] for x in g) and all(y["source_sha256"]==sha(line(y["page"],y["ordinal"])["body"].encode()) for x in g for y in x["members"]))
test("m2_two_literal_scope_bents",line(1,22)["body"].endswith("Ancak aksine bir hüküm yoksa;") and line(1,23)["body"].startswith("a) ") and line(1,26)["body"].startswith("b) "))
test("m47_six_amendment_starts_preserved",all(line(p,o)["raw"].startswith("(") for p,o in [(39,30),(39,33),(40,5),(40,9),(40,18),(40,28)]))
lawnotes=[]
test("law_title_combined_superscript",anchor(1,0,123) and line(1,0)["raw"]=="KARAYOLLARI TRAFİK KANUNU123")
for n,oo in [(1,range(28,36)),(2,range(36,40)),(3,range(40,43))]:
    test(f"law_note_{n}_separate",line(1,oo.start)["raw"].startswith(str(n)+" ") and all(line(1,o)["classification"]=="FOOTNOTE_CANDIDATE" for o in oo) and note(1,oo) in pages[1])
    lawnotes.append({"note":n,"owner":"LAW_TITLE_ONLY","anchor":ev(line(1,0)),"lines":[ev(line(1,o)) for o in oo]})
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
for n in (2,47):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid(n),)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid(n),))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[]
def annotate(n,k,p,o):
    x=line(p,o);d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid(n),k,p,o,x["raw"],sha(x["raw"].encode())));anns.append({"article_id":aid(n),"kind":k,**ev(x)})
for n,p,o in [(1,1,15),(2,1,18),(48,40,32)]:
    assert line(p,o)["classification"]=="HEADING_CANDIDATE" and d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(aid(n),)).fetchone()==(p,o+1)
    annotate(n,"TITLE",p,o)
for n,p,o,oo in [(43,41,0,range(35,37)),(44,41,21,range(37,40))]:
    test(f"note_{n}_m48_anchor",anchor(p,o,n) and line(p,o)["raw"]==line(p,o)["body"]+str(n) and s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone()[0]==aid(48) and line(p,oo.start)["raw"].startswith(str(n)+" ") and all(line(p,j)["classification"]=="FOOTNOTE_CANDIDATE" for j in oo) and note(p,oo) in pages[p])
    for j in oo:annotate(48,"FOOTNOTE",p,j)
test("m47_note42_preserved",anchor(39,20,42) and d.execute("SELECT kind,page,ordinal FROM reader_annotation WHERE article_id=? ORDER BY page,ordinal",(aid(47),)).fetchall()==[("TITLE",39,20),("FOOTNOTE",39,37),("FOOTNOTE",39,38)] and note(39,range(37,39)) in pages[39])
test("eight_new_annotations_no_overlap",len(anns)==8 and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR059' WHERE key='projection_revision'")
test("only_two_status_changes",all(set(x[0] for x in d.execute(q))=={aid(2),aid(47)} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("59_added_no_old_removed",len(added)==59 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and not d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())
test("30_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==30 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("56_exclusions",len(set(excluded+zones))==56 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
test("law_notes_not_article_body_or_annotation",all(not d.execute("SELECT 1 FROM reader_line WHERE law=2918 AND page=1 AND ordinal=?",(o,)).fetchall() and not d.execute("SELECT 1 FROM reader_annotation a JOIN article_index i ON i.id=a.article_id WHERE i.law=2918 AND a.page=1 AND a.ordinal=?",(o,)).fetchall() for o in range(28,43)))
test("90_definition_anchors_preserved",len(J.loads((R/"tur058-boundary-review.json").read_bytes())["definition_anchors"])==90)
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"groups":g,"scope_group":{"lead":[1,22],"members":[[1,23],[1,26]],"legal_paragraph_number":None},"law_title_notes":lawnotes,"annotations":anns,"legal_acceptance":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR059_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1478,"withheld_articles":30,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[aid(2),aid(47)]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
