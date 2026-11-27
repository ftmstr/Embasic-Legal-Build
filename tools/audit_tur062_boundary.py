import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata
def sha(b):return H.sha256(b).hexdigest()
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
def root(n):return next(P.Path(n).rglob("result.json")).parent
R=P.Path("tur062-evidence");R.mkdir()
a,b,c=map(root,("prior61","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2918.pdf";ph="44a3394f7b3e5ad7495d26d862086afc699f7013fadc996be686dc3bee4fd8ce";pin(pdf,ph)
hashes={"review-only.json":"fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747","boundary-review.json":"b43119740aa5b2d463051461348d4962591430fb31a60aa8655db4ab71a98f40","tur052-boundary-review.json":"49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474","tur053-boundary-review.json":"149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776","tur054-boundary-review.json":"3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51","tur055-boundary-review.json":"402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372","tur056-boundary-review.json":"927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c","tur057-boundary-review.json":"360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec","tur058-boundary-review.json":"9cbb679c51a53b061ae4515b4f70799181185f7af73489c69e756e92f7f923b5","tur059-boundary-review.json":"d0c4388cd5d2f5cdc60ce9ab0e6cf4cd146e5ed2051e71bdd780140555ad7d61","tur060-boundary-review.json":"8e55d10f5704ec6d1959885edf20b20d165531a86fb64eca32141989a80485b8"}
for n,h in hashes.items():(R/("tur061-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2918 AND page=? AND ordinal=?",(p,o)).fetchone())
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (92,93)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def e(n):return f"2918:ek:{n}"
prior=J.loads((R/"tur061-boundary-review.json").read_bytes())
note=[line(92,o) for o in range(34,37)];nt=norm(" ".join(x["raw"] for x in note));anchor=line(92,18)
test("note90_source_owner",pages[92].count(nt)==1 and anchor["raw"]==anchor["body"]+"90" and any(z["text"].strip()=="90" and z["flags"]&1 and z["size"]<=9 for z in J.loads(anchor["spans"])) and s.execute("SELECT article_id FROM membership WHERE law=2918 AND page=92 AND ordinal=18").fetchone()[0]==e(18) and prior["note90"]=={"owner":e(18),"anchor":ev(anchor),"source_rows":[ev(x) for x in note]})
chapter=[ev(line(93,o)) for o in (7,8)]
test("chapter_boundary_exact",chapter==prior["chapter"] and norm(" ".join(x["raw"] for x in chapter)+" "+line(93,9)["raw"]) in pages[93])
added=[];evidence=[];historical=[]
for n,coords,excluded in [(19,[(92,33),(93,0),(93,1)],[(92,o) for o in range(34,37)]),(20,[(93,o) for o in range(2,7)],[(93,7)])]:
    rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(e(n),))]
    test(f"ek{n}_historical_coordinates",[(x["page"],x["ordinal"]) for x in rs]==sorted(coords+excluded))
    historical.extend([e(n),2918,x["page"],x["ordinal"],x["body"]] for x in rs)
    good=[x for x in rs if (x["page"],x["ordinal"]) in coords]
    start=good[0]["raw"];stop=line(93,2 if n==19 else 7)["raw"]
    assert pages[good[0]["page"]].count(start)==pages[93].count(stop)==1
    block=pages[92][pages[92].index(start):].replace(nt,"")+" "+pages[93][:pages[93].index(stop)] if n==19 else pages[93][pages[93].index(start):pages[93].index(stop)]
    test(f"ek{n}_complete_independent_pdf_block",norm(block)==norm(" ".join(x["raw"] for x in good)))
    test(f"ek{n}_only_article_label_removed",all(x["body"]==(x["raw"].split("- ",1)[1] if i==0 else x["raw"]) and x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for i,x in enumerate(good)))
    added.extend((e(n),2918,x["page"],x["ordinal"],x["body"]) for x in good)
    evidence.extend({"article_id":e(n),**ev(x)} for x in good)
test("historical_12_rows_preserved",historical==prior["removed_reader_rows"])
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
for n in (19,20):
    assert d.execute("SELECT status FROM article_index WHERE id=?",(e(n),)).fetchone()==("WITHHELD_REVIEW",)
    d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(e(n),))
d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
d.execute("UPDATE metadata SET value='TUR062' WHERE key='projection_revision'")
test("only_two_status_changes",all(set(x[0] for x in d.execute(q))=={e(19),e(20)} for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("eight_added_no_removal",len(added)==8 and set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and not d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())
test("25_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==25 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
test("98_annotations_exact_and_note90_separate",d.execute("SELECT count(*) FROM reader_annotation").fetchone()[0]==98 and all(not d.execute(q).fetchall() for q in ("SELECT * FROM reader_annotation EXCEPT SELECT * FROM old.reader_annotation","SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation")) and d.execute("SELECT kind,page,ordinal FROM reader_annotation WHERE article_id=? ORDER BY page,ordinal",(e(18),)).fetchall()==[("TITLE",92,9)]+[("FOOTNOTE",92,o) for o in range(34,37)] and not d.execute("SELECT 1 FROM reader_annotation WHERE article_id IN (?,?)",(e(19),e(20))).fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]+[(2918,92,o) for o in range(34,37)]+[(2918,93,7)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("60_exclusions_and_chapter_second_line",len(set(excluded+zones))==60 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones+[(2918,93,8)]))
rv=J.loads((R/"review-only.json").read_bytes())
test("grammar_definition_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and len(J.loads((R/"tur058-boundary-review.json").read_bytes())["definition_anchors"])==90 and all(not d.execute(q).fetchall() for q in ("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision","SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision")) and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":evidence,"chapter":chapter,"note90":prior["note90"],"legal_acceptance":False,"automatic_effect":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR062_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1483,"withheld_articles":25,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[e(19),e(20)]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
