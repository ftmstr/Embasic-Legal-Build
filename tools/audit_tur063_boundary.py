import pathlib as P,sqlite3 as S,hashlib as H,json as J,shutil,subprocess,unicodedata,re
def sha(b):return H.sha256(b).hexdigest()
def pin(p,h):
    b=p.read_bytes();assert sha(b)==h,p;return b
def root(n):return next(P.Path(n).rglob("result.json")).parent
R=P.Path("tur063-evidence");R.mkdir()
a,b,c=map(root,("prior62","prior47","prior45"))
base=a/"reader-candidate.sqlite";pin(base,"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8")
src=b/"grammar-candidate.sqlite";pin(src,"3945755b862a41a2b510c1afe78c1ab758f8d4e2dac70d37534082fa44805b7e")
pdf=c/"new-raw/2863.pdf";ph="6f0f0cce24e5e3505b5c368a74e9305329295ae325f49b62560e2eee167f1b38";pin(pdf,ph)
hashes={"review-only.json":"fe586ceb946df3ecabf7000029f14f704cbbc78ff16904e0bc88c33a4278e747","boundary-review.json":"7ec6274d8a6e7cd57bfc3d7015b1c15b41b4af7b560a16f6c045fabe7717d8ea","tur052-boundary-review.json":"49277893e2f15ea0c99482b3c47fa843434ffe8419dc4ec0da82b03d1561c474","tur053-boundary-review.json":"149e26cd5edcbc8c3d8314f0b75732049264f6f1898ea97144d146aa6d598776","tur054-boundary-review.json":"3e24fe65dba95b3c65f3afe97cfb2c73dd8e8099ae57b7130ec489c4342b2d51","tur055-boundary-review.json":"402918e1ff500bfd69c4528afb2a2f607344fefbfc4b74b88a64177b3bf32372","tur056-boundary-review.json":"927a77ca32f5938c63d4c8e2798472dd14d216fb12a64337705c82cf5371c86c","tur057-boundary-review.json":"360d0ae8ad401a02862adffcc8baae6348651a61111e26e948a70cfb483c37ec","tur058-boundary-review.json":"9cbb679c51a53b061ae4515b4f70799181185f7af73489c69e756e92f7f923b5","tur059-boundary-review.json":"d0c4388cd5d2f5cdc60ce9ab0e6cf4cd146e5ed2051e71bdd780140555ad7d61","tur060-boundary-review.json":"8e55d10f5704ec6d1959885edf20b20d165531a86fb64eca32141989a80485b8","tur061-boundary-review.json":"b43119740aa5b2d463051461348d4962591430fb31a60aa8655db4ab71a98f40"}
for n,h in hashes.items():(R/("tur062-boundary-review.json" if n=="boundary-review.json" else n)).write_bytes(pin(a/n,h))
s=S.connect(src.resolve().as_uri()+"?mode=ro",uri=True);s.row_factory=S.Row
def norm(t):return " ".join(unicodedata.normalize("NFC",t).split())
def line(p,o):return dict(s.execute("SELECT * FROM line WHERE law=2863 AND page=? AND ordinal=?",(p,o)).fetchone())
def ev(x):return {"page":x["page"],"ordinal":x["ordinal"],"raw":x["raw"],"bbox":J.loads(x["bbox"]),"raw_sha256":sha(x["raw"].encode())}
def block(coords):return norm(" ".join(line(p,o)["raw"] for p,o in coords))
def anchor(p,o,k):return any(z["text"].strip()==str(k) and z["flags"]&1 and z["size"]<=9 for z in J.loads(line(p,o)["spans"]))
pages={p:norm(subprocess.check_output(["pdftotext","-f",str(p),"-l",str(p),"-layout",str(pdf),"-"]).decode()) for p in (1,2,3,4)}
checks=[]
def test(n,v):
    assert v,n
    checks.append({"case":n,"pass":True})
def aid(n):return f"2863:normal:{n}"
m=aid(3);coords=[(1,o) for o in range(21,31)]+[(2,o) for o in range(38)]+[(3,o) for o in range(34)]
rs=[dict(x) for x in s.execute("SELECT m.*,l.raw,l.bbox,l.classification FROM membership m JOIN line l USING(law,page,ordinal) WHERE article_id=? ORDER BY m.page,m.ordinal",(m,))]
test("82_exact_coordinates",[(x["page"],x["ordinal"]) for x in rs]==coords and all(x["classification"] in ("ARTICLE_HEADER","BODY_CANDIDATE") for x in rs))
for p,start,end,following in [(1,21,30,31),(2,0,37,38),(3,0,33,34)]:
    starttext=line(p,start)["raw"];endtext=line(p,following)["raw"]
    assert pages[p].count(starttext)==pages[p].count(endtext)==1
    test(f"page_{p}_complete_block",norm(pages[p][pages[p].index(starttext):pages[p].index(endtext)])==block([(p,o) for o in range(start,end+1)]))
test("only_header_and_note3_removed",anchor(2,3,3) and all(x["body"]==(x["raw"].split(" – ",1)[1] if i==0 else x["raw"][:-1] if (x["page"],x["ordinal"])==(2,3) else x["raw"]) for i,x in enumerate(rs)))
groups=[]
for label,lead,items in [("a",(1,22),[(1,23),(1,27),(1,30),(2,4),(2,7),(2,9),(2,11),(2,16),(2,29),(2,34),(3,2),(3,7),(3,11),(3,14),(3,17),(3,22)]),("b",(3,28),[(3,29),(3,30),(3,31)])]:
    parent=m+"/b:"+label
    overlay=s.execute("SELECT new_id,parent_id,kind,status FROM grammar_overlay WHERE law=2863 AND page=? AND ordinal=?",lead).fetchone()
    test(label+"_literal_group_parent",line(*lead)["raw"].startswith(label+") ") and tuple(overlay)==(parent,m,"clause_candidate","REVIEW_REQUIRED"))
    members=[]
    for i,(p,o) in enumerate(items,1):
        x=line(p,o);overlay=s.execute("SELECT new_id,parent_id,kind,status FROM grammar_overlay WHERE law=2863 AND page=? AND ordinal=?",(p,o)).fetchone()
        assert x["raw"].startswith(f"({i}) ") and tuple(overlay)==(parent+f"/s:{i}",parent,"subclause_candidate","REVIEW_REQUIRED")
        members.append({"literal_label":f"({i})",**ev(x)})
    test(label+"_nested_number_sequence",len(members)==len(items))
    groups.append({"literal_label":label+")","lead":ev(line(*lead)),"members":members,"legal_paragraph_number":None})
test("all_21_overlay_anchors_accounted",s.execute("SELECT count(*) FROM grammar_overlay WHERE law=2863").fetchone()[0]==21 and sum(len(g["members"]) for g in groups)==19)
amendments=[ev(x) for x in rs if re.search(r"\((?:Değişik|Ek):",x["raw"])]
test("14_amendment_anchors_retained",len(amendments)==14) 
lawnotes=[]
test("law_title_combined_superscript",anchor(1,0,12) and line(1,0)["raw"]==line(1,0)["body"]+"12")
for num,oo in [(1,range(31,39)),(2,range(39,43))]:
    test(f"law_note{num}_separate",line(1,oo.start)["raw"].startswith(str(num)+" ") and all(line(1,o)["classification"]=="FOOTNOTE_CANDIDATE" for o in oo) and block([(1,o) for o in oo]) in pages[1])
    lawnotes.append({"note":num,"owner":"LAW_TITLE_ONLY","anchor":ev(line(1,0)),"lines":[ev(line(1,o)) for o in oo]})
note3=[line(2,o) for o in range(38,41)]
test("note3_sit_owner",anchor(2,3,3) and line(2,3)["raw"]=="alanlardır.3" and line(1,30)["raw"].startswith('(3) "Sit";') and note3[0]["raw"].startswith("3 ") and all(x["classification"]=="FOOTNOTE_CANDIDATE" for x in note3) and block([(2,o) for o in range(38,41)]) in pages[2])
dst=R/"reader-candidate.sqlite";shutil.copyfile(base,dst);d=S.connect(dst);d.execute("PRAGMA foreign_keys=ON");d.execute("ATTACH DATABASE ? AS old",(base.resolve().as_uri()+"?mode=ro",))
for n,cc in [(1,[(1,o) for o in range(14,17)]),(2,[(1,18),(1,19)]),(4,[(3,o) for o in range(35,40)]+[(4,o) for o in range(8)])]:
    rr=d.execute("SELECT page,ordinal,body FROM reader_line WHERE article_id=? ORDER BY page,ordinal",(aid(n),)).fetchall()
    test(f"neighbor_m{n}_exact_body_boundaries",[(p,o) for p,o,t in rr]==cc and all(t==(line(p,o)["raw"].split(" – ",1)[1] if i==0 else line(p,o)["raw"]) for i,(p,o,t) in enumerate(rr)) and all(block([(p,o) for q,o in cc if q==p]) in pages[p] for p in {p for p,o in cc}))
assert d.execute("SELECT status FROM article_index WHERE id=?",(m,)).fetchone()==("WITHHELD_REVIEW",)
d.execute("UPDATE article_index SET status='CANDIDATE_UNVERIFIED',reason='SOURCE_BOUNDARY_REVIEW_ONLY' WHERE id=?",(m,))
added=[(m,2863,x["page"],x["ordinal"],x["body"]) for x in rs];d.executemany("INSERT INTO reader_line VALUES(?,?,?,?,?)",added)
anns=[]
for n,k,p,o in [(1,"TITLE",1,13),(2,"TITLE",1,17),(3,"TITLE",1,20),(4,"TITLE",3,34)]+[(3,"FOOTNOTE",2,o) for o in range(38,41)]:
    x=line(p,o)
    if k=="TITLE":assert x["classification"]=="HEADING_CANDIDATE" and d.execute("SELECT header_page,header_ordinal FROM article_index WHERE id=?",(aid(n),)).fetchone()==(p,o+1)
    d.execute("INSERT INTO reader_annotation VALUES(?,?,?,?,?,?)",(aid(n),k,p,o,x["raw"],sha(x["raw"].encode())));anns.append({"article_id":aid(n),"kind":k,**ev(x)})
test("seven_annotations_no_overlap",len(anns)==7 and not d.execute("SELECT * FROM old.reader_annotation EXCEPT SELECT * FROM reader_annotation").fetchall() and not d.execute("SELECT 1 FROM reader_line r JOIN reader_annotation a ON r.law=(SELECT law FROM article_index WHERE id=a.article_id) AND r.page=a.page AND r.ordinal=a.ordinal").fetchall())
d.execute("UPDATE metadata SET value='TUR063' WHERE key='projection_revision'")
test("only_one_status_change",all(d.execute(q).fetchall()==[(m,)] for q in ("SELECT id FROM (SELECT * FROM article_index EXCEPT SELECT * FROM old.article_index)","SELECT id FROM (SELECT * FROM old.article_index EXCEPT SELECT * FROM article_index)")))
test("82_added_no_removed",set(d.execute("SELECT * FROM reader_line EXCEPT SELECT * FROM old.reader_line").fetchall())==set(added) and not d.execute("SELECT * FROM old.reader_line EXCEPT SELECT * FROM reader_line").fetchall())
test("24_held_no_leak",d.execute("SELECT count(*) FROM article_index WHERE status='WITHHELD_REVIEW'").fetchone()[0]==24 and not d.execute("SELECT * FROM reader_line WHERE article_id IN (SELECT id FROM article_index WHERE status='WITHHELD_REVIEW')").fetchall())
excluded=[(2918,98,o) for o in range(34,37)]+[(2918,99,o) for o in (0,1)]+[(2918,43,o) for o in (32,33)]+[(2918,26,o) for o in (26,27,35)]+[(2918,7,o) for o in (21,22)]+[(2918,37,o) for o in range(12,16)]+[(2918,2,o) for o in (2,3)]+[(2918,92,o) for o in range(34,37)]+[(2918,93,7)]
zones=list(map(tuple,s.execute("SELECT m.law,m.page,m.ordinal FROM membership m JOIN source_zone z USING(law,page,ordinal)")))
test("60_exclusions_plus_chapter_second_line",len(set(excluded+zones))==60 and all(not d.execute("SELECT 1 FROM reader_line WHERE law=? AND page=? AND ordinal=?",(l,p,o)).fetchall() for l,p,o in excluded+zones+[(2918,93,8)]))
test("law_title_notes_no_article_assignment",all(not d.execute("SELECT 1 FROM reader_line WHERE law=2863 AND page=1 AND ordinal=?",(o,)).fetchall() and not d.execute("SELECT 1 FROM reader_annotation a JOIN article_index i ON i.id=a.article_id WHERE i.law=2863 AND a.page=1 AND a.ordinal=?",(o,)).fetchall() for o in range(31,43)))
rv=J.loads((R/"review-only.json").read_bytes())
test("grammar_definition_temporal_preserved",len(rv["overlay_review"])==119 and len(rv["list_review"])==19 and sum(len(x["members"]) for x in rv["list_review"])==90 and len(J.loads((R/"tur058-boundary-review.json").read_bytes())["definition_anchors"])==90 and all(not d.execute(q).fetchall() for q in ("SELECT * FROM temporal_decision EXCEPT SELECT * FROM old.temporal_decision","SELECT * FROM old.temporal_decision EXCEPT SELECT * FROM temporal_decision")) and d.execute("SELECT count(*) FROM temporal_decision").fetchone()[0]==11 and all(not J.loads(x[0])["automatic_effect"] for x in d.execute("SELECT record FROM temporal_decision")))
test("ktk187_and_2863_98_candidate",all(d.execute("SELECT status,count(*) FROM article_index WHERE law=? GROUP BY status",(law,)).fetchall()==[("CANDIDATE_UNVERIFIED",n)] for law,n in [(2918,187),(2863,98)]))
d.commit();test("integrity",d.execute("PRAGMA integrity_check").fetchone()==("ok",) and not d.execute("PRAGMA foreign_key_check").fetchall())
counts={t:d.execute("SELECT count(*) FROM "+t).fetchone()[0] for t in ("article_index","reader_line","reader_annotation","temporal_decision")}
d.close();s.close()
(R/"boundary-review.json").write_text(J.dumps({"pdf_sha256":ph,"body_coordinates":[ev(x) for x in rs],"nested_groups":groups,"amendment_anchors":amendments,"law_title_notes":lawnotes,"note3":{"owner":m,"literal_group":"a)","literal_item":"(3)","anchor":next(ev(x) for x in rs if (x["page"],x["ordinal"])==(2,3)),"source_rows":[ev(x) for x in note3]},"annotations":anns,"legal_acceptance":False,"automatic_effect":False},ensure_ascii=False,indent=2),encoding="utf-8")
out={"status":"TUR063_BOUNDARY_CANDIDATE_ONLY","promotion_allowed":False,"production":"NO-GO","official_legal_source":"NOT_VERIFIED","physical_uat":"NOT_RUN","counts":counts,"candidate_articles":1484,"withheld_articles":24,"reader_sha256":sha(dst.read_bytes()),"pdf_sha256":ph,"checks":checks,"opened":[m]}
(R/"result.json").write_text(J.dumps(out,ensure_ascii=False,indent=2),encoding="utf-8");print(J.dumps(out))
