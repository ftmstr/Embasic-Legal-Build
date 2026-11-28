"""TUR050 audit-only integration boundary. Trust this exact reviewed Git source
independently of the supplied DB. Pins are not signatures or legal approval."""
import argparse,json,sys,sqlite3
from audit_candidate_reader import snapshot,read_article,Denied
PINS={
 "TUR093":{"sha256":"9faa2dd6cff4429366110775e64bc4896b46c902b773b69c31ff8f95fab24354","artifact":15271042600,"run":46164837395,"head":"1c861b9b67da6710f4caaa8b6940a805aad19714"},
 "TUR092":{"sha256":"a0771e9dc7d8eddfb5bf629bf85374520ac477dcb7472bdfdb0ede63a11dbf12","artifact":15270866354,"run":46163947807,"head":"a2190e1721ffb285a3f95a3cc2b51aac6a35c886"},
 "TUR091":{"sha256":"f08eaf501479f38a2be8debbdd85e75b4fb29d25158d494b280ee9bd227535b8","artifact":15270342820,"run":46163283889,"head":"d507f8704cd24d400333c246536bd017ef1e038a"},
 "TUR090":{"sha256":"7b27ad1b55974147edb60d19fdbd32a25e41c3b6ec88a1c2c8a9e8e30c6d906e","artifact":15269734497,"run":46162612124,"head":"ebc9af2126e2716653cb608e9f141688280d7236"},
 "TUR089":{"sha256":"d1aefc49299e45791a9892f41898ef3c8e8e6e0a8165b3ec6983e7683848cf25","artifact":15270172203,"run":46161909023,"head":"a9af58d2bd16a68b0d7beba28a7217b2a85b41e8"},
 "TUR088":{"sha256":"49c0dacda1c8c55e22ab9509da249ee11c5ec7a2884676a2072b05b574d40a50","artifact":15270206098,"run":46161293930,"head":"3e7dfe78470bc61aa2640e11ed694b245fed2a5c"},
 "TUR087":{"sha256":"9d5e5340b91ad00a2b76f2f9bcc377af11019638dfff2203f919a3d69fbbd601","artifact":15269205908,"run":46158676390,"head":"8dcb442eb1ab86d726b57f29a9ec2d3da6607e79"},
 "TUR085":{"sha256":"e054a71f0f62a0706c4a6168971eac6a96073b9f6d9879512e8dc52b76f1daf5","artifact":15267907580,"run":46156664727,"head":"ca006df8888d06d667d4318c2695dd517d6bf4d4"},
 "TUR084":{"sha256":"8c0bbe61a30566167fabf50cf2185b6cd8bc545279605813bcd27fc6b1305eb0","artifact":15268035068,"run":46155683671,"head":"8aefccd486f3c45f4df7933cf60c52d9f7f60beb"},
 "TUR083":{"sha256":"70dd443f64bd614642cbb338bee8ee6a71ad31b897dff0515e8550e95f9e1fa2","artifact":15258444815,"run":46133447598,"head":"3fc04be4143322dd0a7978a585fd08d2b35d11bb"},
 "TUR082":{"sha256":"e0cbfb9a807b0edbeebdfd434208148f43a731d274f82c490aab3b39da17fcea","artifact":15257832912,"run":46132554106,"head":"155eedf3ca6323be4629583e54cf8dfbf569cd48"},
 "TUR081":{"sha256":"fa4a2da5ed164fb70861c573200832832415989d3f771c1235203eb04024fac5","artifact":15255977897,"run":46126985221,"head":"49169cd9ed1b021c5861847ba7093845720397fe"},
 "TUR080":{"sha256":"3e46cf18e90c9a95175ae2e073d31ef41a8ebea86b63f39535c5bee7f417509b","artifact":15254404769,"run":46125710964,"head":"c37e0757b9fa7af933a41b451f77f879cfc2c395"},
 "TUR079":{"sha256":"902f0a2627a1ac8479aa8e6f8c01af539d885b9f51c139874622bc30ba9a80e9","artifact":15254504533,"run":46124746447,"head":"6d9246784a6519e9394f013c790c0647cd65d4d9"},
 "TUR078":{"sha256":"154b159f70ead930d4a5e3716b29394a4cbb3e07e8022a4d58247d9cd68f238d","artifact":15254518103,"run":46123967698,"head":"98a590dc434300fc42cab7a3a07978300f05bf7e"},
 "TUR077":{"sha256":"09d9ec749f4b1edb54ebb8b127d32a5347a19cc435f2a4bcb58becc97bfae6be","artifact":15255100402,"run":46123379598,"head":"5db9684b2ccbcac9c7f6927ad270affadb669f65"},
 "TUR076":{"sha256":"438ae4e05c5deae7d462f78eb3e7605709c01b74e0fa04dd03c2282f90a6c4c1","artifact":15233040668,"run":46070012721,"head":"402034848a54ed7758ff741e4bcd52971bcee6ae"},
 "TUR075":{"sha256":"38e56b5f5cdc955237f370b469ddce165025b1cc43202ee4d8480b354f0a74cc","artifact":15232845073,"run":46069222176,"head":"742971024eda01577541c823f388022dcc29a95e"},
 "TUR074":{"sha256":"4e0d669dffcbeba13134312cc98c47a127bd57910547aa98cff72e8a52a20da2","artifact":15230408262,"run":46065997158,"head":"4fb098a6b10187a5ac520fc518e9ef8f3ccdfea7"},
 "TUR073":{"sha256":"564eaa8da716b6909254e0fce434d34b80430dd57564c81b4410ea6125dc7009","artifact":15229556899,"run":46062886728,"head":"6ef1bfcbdf0d4fbf796f53c565b286fcbe12e6c4"},
 "TUR072":{"sha256":"1711398c35ae71f11862e53310a2118737d6308a61f26f07b3275f42eaf58f29","artifact":15229098262,"run":46062006239,"head":"d8ff8a933d56d87e6408dcb97663749498677573"},
 "TUR071":{"sha256":"c6f06f2c5665bda680d2e579990557518706c097b5c90377b195af5acf75229d","artifact":15227278342,"run":46058481407,"head":"352c4d38341220e8e1ca1a7a42a4b64206dbaae1"},
 "TUR070":{"sha256":"00e7b912cba49e773cb70879406a3b0d76007c87449954c584b585bf07fb6b3c","artifact":15226094332,"run":46057749840,"head":"16eec6577d64315b7bdf774e003bc6f67db77eb1"},
 "TUR069":{"sha256":"55fc5dfa76c763d5ccd9f31359b6d918865024603bb28627043b6e00dc587e22","artifact":15226253510,"run":46056939459,"head":"bf47618a66aa67dcd5937c273955bc9ab6c03b2a"},
 "TUR068":{"sha256":"807cf592a55bb5b6fba60d1f678ed489d1d8bfc343e4f9f8883499a977f77fea","artifact":15226202525,"run":46056242011,"head":"8c3afa6a36a48eec0796ca511cb6c6a28061c6f0"},
 "TUR067":{"sha256":"7169e03e4fa671286c06397d9b0955d70b5da726449201420317f112b0a99e34","artifact":15225899094,"run":46055412596,"head":"b5937140fcd52d1ff701316453bac3ced6329c85"},
 "TUR066":{"sha256":"c8245dc5d5824798c5f47a95bbf0ac21c1883ad4fcd2e421abc45615bd60a65a","artifact":15223381987,"run":46047479451,"head":"1c9748e35d4d31cfc70ebda37dedd0235f13fa2b"},
 "TUR063":{"sha256":"cd773dcd4f2eb52d3c2ac3e860b2682b233a89784ddd36d2050a41afcb823b06","artifact":15220742803,"run":46043316831,"head":"59d9f8cb991ba6e820f1dcfeca1b269a8a71922a"},
 "TUR062":{"sha256":"0f0ec10ae318aacf16da53f02a7a1a59e204f21dc02fd8d00561ddeb5a8f37a8","artifact":15191499573,"run":45987493698,"head":"ba600ca1ecac739b17afd9b0c8d2cae27a93e289"},
 "TUR061":{"sha256":"cbebc8caa46e94a91f4ecfba18f632f0829e7530a66b46255c34b9181eac54fc","artifact":15156381649,"run":45901669617,"head":"6c211dcde4b1d77b4352aac88059112d356086dc"},
 "TUR060":{"sha256":"8257ab81c87adf193892102e41647e7ef9384f522b4288ce3cc648b9bfd7ed65","artifact":15144913108,"run":45876381548,"head":"66b74ca3702c673f4fac3e5a3831936768474185"},
 "TUR059":{"sha256":"a3f7b752c838d04359a2ef9fdfe9df96bffac48f5c03a59629aee167968de884","artifact":15137949379,"run":45862843182,"head":"e1bbe15a26e88917a9df4fe278f8f9b765a47ece"},
 "TUR058":{"sha256":"d702d09d142ed2555a1c8d47e2602b5ca5983225300bdd6e9ab9e7990c8e3200","artifact":15124392385,"run":45834946840,"head":"2c5d714297cd7815b16bc03e777c9d7f28c8fb30"},
 "TUR057":{"sha256":"028cabe3d0e88fa75b682fd8c869006863a589dbda3700e46f4e87afc73be4bf","artifact":15093895209,"run":45769614123,"head":"07fcb280f84741b05afe63ae15124a0d2530bd5d"},
 "TUR056":{"sha256":"c7424fdeedcc46d93865f48f66077f8144ef0c5b0fbf4531c8cef379372d7c38","artifact":15093086567,"run":45768293248,"head":"69e9c860fe5b5dfe94c85f4b0c9040ca2a199444"},
 "TUR055":{"sha256":"91f7369ee251f7897719e4e4bb4cd80ff2af04ef85081294852fa78b60a77e2d","artifact":15064090183,"run":45700585363,"head":"523e7e5e602669d171d7f869753df894eeb3d6ec"},
 "TUR054":{"sha256":"643a3f1b65299dea8bc5d2edb56edc90138b643b0dddb323200e727c1236d2cc","artifact":15060707849,"run":45696360940,"head":"f942494456b3f63c91e8e7937375628be7700bec"},
 "TUR053":{"sha256":"8cb48e72aaba7d25f902618c47edd26ef51e7a7d61182dc0610e8f98a0137b4c","artifact":15060641088,"run":45694684288,"head":"9954c422be0810d4534c7055a707cb758ca8a932"},
 "TUR052":{"sha256":"2d5303313a2886918a5506ee516398b745cb42cb102a8d233c12f5cd0e822818","artifact":15060085952,"run":45693327586,"head":"444ebcc0f6104f6a1fa9481c37c18f0a440b7a5e"},
 "TUR051":{"sha256":"6e8d9b2b12295cb5ebfeca98d3bf6921955a2d6e123b2567cd183b96b7fb5ab5","artifact":15058496001,"run":45689770047,"head":"f3b1766579d4e120bf1cbb3e3da15fe2379c13b1"},
 "TUR049":{"sha256":"8cf36bf2937f7c73020d9bd6975ec9a2d75b84d0d45d42fe99a0582758f18919","artifact":15057816038,"run":45687953803,"head":"bfd55c2326dba752a8cc8ae54bbed8b7049f356d"},
 "TUR050":{"sha256":"a884a9e4b26977c75b1baec3dddefde4ade3426719d3f9b0e44c82d2c87eb1a9","artifact":15056664563,"run":45688754638,"head":"a7982a034e24daec6b2d07713086be2b4903cf6a"}
}
def main():
    p=argparse.ArgumentParser(description=__doc__,allow_abbrev=False)
    p.add_argument("--db",required=True);p.add_argument("--revision",required=True,choices=tuple(PINS));p.add_argument("--article",required=True)
    a=p.parse_args();pin=PINS[a.revision]
    try:
        db=snapshot(a.db,pin["sha256"])
        try:
            meta=dict(db.execute("SELECT key,value FROM metadata"))
            if meta.get("projection_revision","TUR049")!=a.revision:raise Denied("REVISION_MISMATCH")
            v=read_article(db,a.article)
        finally:db.close()
        v["evidence"]={"revision":a.revision,**pin}
        print(json.dumps(v,ensure_ascii=False));return 0
    except (Denied,OSError,sqlite3.Error,ValueError) as e:
        print(json.dumps({"status":"DENIED","reason":str(e) if isinstance(e,Denied) else type(e).__name__,"promotion_allowed":False}),file=sys.stderr);return 65
if __name__=="__main__":sys.exit(main())
