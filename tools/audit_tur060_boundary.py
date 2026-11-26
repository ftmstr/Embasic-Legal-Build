import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata
def sha(b):return H.sha256(b).hexdigest()
def root(n):return next(P.Path(n).rglob("result.json")).parent
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
R=P.Path("tur060-evidence");R.mkdir()
a,b,c=map(root,("prior59","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
for n,h in [("review-only.json","fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747"),("boundary-review.json","d0c4388cd5d2f5cdc60ce9ab0e6cf4cd146e5ed2051e71bdd780140555ad7d61"),("tur052-boundary-review.json","49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474"),("tur053-boundary-review.json","149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776"),("tur054-boundary-review.json","3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51"),("tur055-boundary-review.json","402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372"),("tur056-boundary-review.json","927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c"),("tur057-boundary-review.json","360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec"),("tur058-boundary-review.json","9cbb679c51a53b061ae4515b4f70799181185f7af73489c69e756e92f7f923b5")]:
    (R/("tur059-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (53,54,55,87,88)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
def anchor(p,o,n):
    return any(z["text"].strip()==str(n) and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(p,o)["spans"]))
def note(p,oo):return norm(" ".join(line(p,o)["raw"] for o in oo))
m="2918:normal:65";e="2918:ek:2"
rv=J.loads((R/"review-only.json").read_bytes());added=[];evidence=[];groups=[]
marks={(53,26):"50",(54,2):"50",(55,6):"51",(55,8):"5253",(55,9):"54",(55,10):"55",(55,13):"56",(87,22):"88"}
for aid,pp,coords,stop,spec in [(m,[53,54,55],[(53,o) for o in range(25,33)]+[(54,o) for o in range(39)]+[(55,o) for o in range(18)],18,[(53,26,"abcdefghijkl"),(54,29,"abcde")]),(e,[87,88],[(87,o) for o in range(4,35)]+[(88,0)],1,[(87,10,"abc"),(87,25,"abc")])]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(aid,))]
    test(aid+"_exact_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in rs))
    texts=[pages[p] for p in pp];start=rs[0]["raw"];end=line(pp[-1],stop)["raw"]
    assert texts[0].count(start)==texts[-1].count(end)==1
    texts[0]=texts[0][texts[0].index(start):];texts[-1]=texts[-1][:texts[-1].index(end)]
    for j,p in enumerate(pp[:-1]):
        ft=norm(" ".join(x[0] for x in s.execute("SELECT raw FROM line WHERE law=2918 AND page=? AND classification='FOOTNOTE_CANDIDATE' ORDER BY ordinal",(p,))))
        if ft:
            assert texts[j].count(ft)==1
            texts[j]=texts[j].replace(ft,"")
    test(aid+"_complete_independent_pdf_block",norm(" ".join(texts))==norm(" ".join(x["raw"] for x in rs)))
    for i,x in enumerate(rs):
        p,o=x["page"],x["ordinal"];raw=x["raw"]
        if i==0: expected=raw.split(" – ",1)[1]
        elif (p,o) in marks:
            k=marks[p,o];assert anchor(p,o,k) and raw.count(k)==1
            expected=raw.replace(k,"",1)
        else:expected=raw
        assert x["body"]==expected,(p,o)
    test(aid+"_header_and_only_confirmed_superscripts_removed",True)
    g=[x for x in rv["list_review"] if x["article"]==aid]
    test(aid+"_literal_groups",[(x["lead_page"],x["lead_ordinal"],"".join(y["literal_label"] for y in x["members"])) for x in g]==spec and all(x["legal_paragraph_number"] is None and x["lead_text"]==s.execute("SELECT body FROM membership WHERE article_id=? AND page=? AND ordinal=?",(aid,x["lead_page"],x["lead_ordinal"])).fetchone()[0] for x in g) and all(y["source_sha256"]==sha(line(y["page"],y["ordinal"])["body"].encode()) for x in g for y in x["members"]))
    groups.extend(g);added.extend((aid,2918,x["page"],x["ordinal"],x["body"]) for x in rs)
    evidence.extend({"article_id":aid,**ev(x)} for x in rs)
test("explicit_normal_65A_and_ek_boundary",line(55,19)["raw"].startswith("Madde 65/A") and line(87,2)["raw"].startswith("Ek Madde 1") and line(87,4)["raw"].startswith("Ek Madde 2") and line(88,2)["raw"].startswith("Ek Madde 3") and len(groups)==4 and sum(len(g["members"]) for g in groups)==23)
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
for aid in (m,e):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(aid,)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(aid,))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[];notes=[]
def annotate(aid,k,p,o):
    x=line(p,o);d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid,k,p,o,x["raw"],sha(x["raw"].encode())));anns.append({"article_id":aid,"kind":k,**ev(x)})
for aid,p,o in [(m,53,24),("2918:normal:65/A",55,18),("2918:ek:1",87,1),(e,87,3),("2918:ek:3",88,1)]:
    assert line(p,o)["classification"]=="HEADING_CANDIDATE" and d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(aid,)).fetchone()==(p,o+1)
    annotate(aid,"TITLE",p,o)
for aid,n,p,oo,anchors in [(m,49,53,range(33,35),[(53,24,"49")]),(m,50,53,range(35,39),[(53,26,"50"),(54,2,"50")]),(m,51,55,range(26,29),[(55,6,"51")]),(m,52,55,range(29,31),[(55,8,"5253")]),(m,53,55,range(31,34),[(55,8,"5253")]),(m,54,55,range(34,36),[(55,9,"54")]),(m,55,55,range(36,39),[(55,10,"55")]),(m,56,55,range(39,42),[(55,13,"56")]),(e,88,87,range(35,38),[(87,22,"88")])]:
    test(f"note_{n}_source_ownership",all(anchor(q,o,k) for q,o,k in anchors) and line(p,oo.start)["raw"].startswith(str(n)+" ") and all(line(p,o)["classification"]=="FOOTNOTE_CANDIDATE" for o in oo) and note(p,oo) in pages[p])
    for o in oo:annotate(aid,"FOOTNOTE",p,o)
    notes.append({"article_id":aid,"note":n,"anchors":[ev(line(q,o)) for q,o,k in anchors],"note_coordinates":[[p,o] for o in oo],"automatic_effect":False})
test("30_annotations_preserve_previous",len(anns)==30 and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR060' WHERE key='projection_revision'")
test("only_two_status_changes",all(set(x[0] for x in d.execute(q))=={m,e} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("97_added_no_removed",len(added)==97 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and not d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())
test("28_held_no_body",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==28 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("56_exclusions",len(set(excluded+zones))==56 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones))
test("90_definition_anchors_preserved",len(J.loads((R/"tur058-boundary-review.json").read_bytes())["definition_anchors"])==90)
test("review_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and not d.execute("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision").fetchall() and not d.execute("SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision").fetchall() and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"groups":groups,"annotations":anns,"notes":notes,"legal_acceptance":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR060_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1480,"withheld_articles":28,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[m,e]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
