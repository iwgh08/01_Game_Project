using System.Linq;
using UnityEditor;
using UnityEngine;

// 씬 생성기가 쓰는 아이템·문서 에셋 묶음
public class GameContent
{
    public ItemData flashlight, musicKey, fuse, broadcastKey;
    public DocumentData attendance, seatChart, diary, photo, sheetMusic, musicBoard, numberPoster,
        broadcastRules, broadcastLog, patrolLog, tapeTranscript, noteRumor, noteTest, noteAudition;
    public string[] tapeLines;
}

// 아이템·문서 에셋(Assets/Data)을 만든다. 다시 실행하면 글 내용을 덮어쓴다.
public static class GameContentBuilder
{
    const string ItemFolder = "Assets/Data/Items";
    const string DocumentFolder = "Assets/Data/Documents";

    static readonly Color NotebookPaper = new(0.95f, 0.93f, 0.86f);
    static readonly Color OldPaper = new(0.86f, 0.82f, 0.71f);
    static readonly Color PrintPaper = new(0.92f, 0.9f, 0.84f);

    // 2학년 3반 출석부 (가나다순)
    public static readonly string[] Students =
    {
        "강민지", "김도윤", "김하늘", "남궁현", "류지아", "문태식", "박서연", "배준호", "송유리", "안재민",
        "오승현", "윤채원", "이도현", "이수빈", "임하준", "장예린", "한서진", "허민혁", "홍다은", "황지훈"
    };

    // 자리 배치 [앞줄→뒷줄, 복도 쪽→창문 쪽]. 17번(한서진)은 넷째 줄 셋째 칸.
    public static readonly int[,] Seats =
    {
        { 5, 12, 1, 9 },
        { 14, 3, 18, 7 },
        { 10, 20, 6, 15 },
        { 2, 11, 17, 4 },
        { 16, 8, 19, 13 }
    };

    public static GameContent Build()
    {
        BuilderKit.EnsureFolder(ItemFolder);
        BuilderKit.EnsureFolder(DocumentFolder);
        var c = new GameContent();

        c.flashlight = Item("Flashlight", "손전등", "경비실 창구에 있던 낡은 손전등. 아직 불이 들어온다. [F]로 켜고 끈다.");
        c.musicKey = Item("MusicRoomKey", "음악실 열쇠", "'음악실'이라고 적힌 꼬리표가 달려 있다. 서진이 사물함에 있었다.");
        c.fuse = Item("Fuse", "퓨즈", "유리관 퓨즈. 피아노 아래 숨겨진 서랍에 들어 있었다.");
        c.broadcastKey = Item("BroadcastRoomKey", "방송실 열쇠", "녹슨 열쇠. 꼬리표에 '방송실'이라고 적혀 있다.");

        c.attendance = Doc("Attendance", "2학년 3반 출석부", AttendanceBody(), false, PrintPaper);
        c.seatChart = Doc("SeatChart", "2학년 3반 자리 배치표", SeatChartBody(), false, PrintPaper);

        c.diary = Doc("Diary", "한서진의 일기장", string.Join("\n---\n",
            "새 학기 첫날!\n\n2학년 3반. 아는 애가 하나도 없어서\n하루 종일 창밖만 봤다.\n\n그런데 짝꿍이 내 필통을 주워 주면서\n\"이거 너 거지?\" 하고 웃었다.\n오늘부터 우리는 친구다.\n\n잊어버리지 않게 사물함 비밀번호를\n우리가 처음 같은 반이 된 오늘 날짜로 바꿨다. 히히.",
            "6월 2일\n\n음악실이 학교에서 제일 좋다.\n아무도 없을 때 피아노를 치면\n학교 전체가 조용히 내 노래를 듣는 것 같다.\n\n노래를 하나 만들고 있다.\n앞부분은 악보에 적어서 사물함에 넣어 두고,\n뒷부분은 음악실 칠판 구석에 적어 놨다.\n(지우지 마세요!! 라고 크게 써 둠)",
            "10월 10일\n\n아빠가 다음 주에 이사 간다고 했다.\n아직 아무한테도 말 못 했다.\n걔한테도.",
            "10월 16일\n\n내일 방과후에 방송실을 빌렸다.\n걔한테 노래를 들려주고, 전학 간다고 말할 거다.\n\n6시 반까지 방송실로 오라고 문자 보내야지.\n안 오면… 그래도 기다릴 거다."), true, NotebookPaper);

        c.photo = Doc("ClassPhoto", "사진 뒷면",
            "2006. 3. 2.\n개학 첫날!\n2학년 3반 다 같이 ♥\n\n\n<size=70%>(앞면: 운동장 앞에서 찍은 반 단체 사진.\n스무 명이 어색하게 웃고 있다.)</size>", true, OldPaper);

        c.sheetMusic = Doc("SheetMusic", "찢어진 악보",
            "서진이의 노래  — 앞부분 —\n\n\n<size=170%>3     5     6</size>\n\n\n\n<size=75%>(뒷부분은 음악실 칠판에!)</size>", true, NotebookPaper);

        c.musicBoard = Doc("MusicBoard", "음악실 칠판 구석의 글씨",
            "<size=80%>지우지 마세요!!</size>\n\n서진이 노래  — 뒷부분 —\n\n\n<size=170%>5     3     2     ‖</size>", true, new Color(0.8f, 0.84f, 0.8f));

        c.numberPoster = Doc("NumberPoster", "숫자로 적는 악보 (숫자보)",
            "음 이름 대신 숫자로 적는 간단한 악보입니다.\n\n<mspace=0.62em>" +
            string.Join("\n", PianoPuzzle.NoteNames.Select((name, i) => $"   {i + 1}   →   {name}")) +
            "</mspace>\n\n<size=85%>· ‖ 는 곡의 끝을 뜻합니다.\n· 숫자 위에 점이 찍혀 있으면 한 옥타브 높은 음입니다.</size>", false, PrintPaper);

        c.broadcastRules = Doc("BroadcastRules", "방송실 사용 수칙",
            "1.  방송부원 외 출입 금지\n\n2.  송출 전에 반드시 '구역 스위치'로\n     방송이 나갈 곳만 켤 것\n\n3.  테이프를 확인한 뒤 송출 버튼을 누를 것\n\n4.  퇴실할 때 배전함 전원을 확인할 것\n\n\n<align=right>방송부 지도교사</align>", false, PrintPaper);

        c.broadcastLog = Doc("BroadcastLog", "방송부 일지 (2006)",
            "10/13 (금)   점심 방송 — 신청곡 3곡\n\n10/16 (월)   아침 조회 방송\n\n10/17 (화)   방과후 개인 방송 신청\n                 2학년 3반 한서진\n                 (담당 선생님 허락. 18:30 ~ 19:00)\n\n10/18 (수)   <color=#8B1A1A>방송실 문 고장. 안에서 열리지 않음.\n                 수리 요청.</color>", false, OldPaper);

        c.patrolLog = Doc("PatrolLog", "야간 순찰 일지",
            "10/12   이상 없음\n\n10/13   2층 창문 잠금 확인\n\n10/15   밤마다 방송실 쪽에서 피아노 소리가 난다는\n            주민 민원. 확인 결과 이상 없음.\n\n10/16   철거 업체 사전 답사\n\n10/17   ", false, PrintPaper);

        c.tapeLines = new[]
        {
            "(지지직… 테이프가 돌아가는 소리)",
            "아, 아. 녹음되고 있나?",
            "…너 기다리다가, 그냥 녹음해 두기로 했어.",
            "나 다음 주에 전학 가. 직접 말하고 싶었는데.",
            "노래 하나 만들었어. 피아노로 친 거야. 웃지 마.",
            "나중에라도 이거 들으면… 우리가 처음 만난 곳이랑, 내가 제일 좋아하던 곳에 틀어 줘.",
            "그럼 나도 어디선가 듣고 있을게.",
            "(한참 동안 아무 소리도 없다)",
            "…이상하다. 문이 안 열려.",
            "밖에… 누구 있어?",
            "(테이프가 끝났다)"
        };
        c.tapeTranscript = Doc("TapeTranscript", "테이프 내용 (받아 적음)", string.Join("\n\n", c.tapeLines), false, NotebookPaper);

        c.noteRumor = Doc("NoteRumor", "구겨진 쪽지",
            "야 서진이 전학 간대\n들었어?\n\n— 헐 진짜? 언제?\n\n— 몰라 다음 주라던데\n걔 아무한테도 말 안 했대", true, NotebookPaper);
        c.noteTest = Doc("NoteTest", "시험지",
            "2학기 중간고사 · 수학\n\n이름: 김하늘\n\n\n<size=220%><color=#B22222>32</color></size>\n\n<color=#B22222>부모님 확인 받아 올 것!</color>", false, PrintPaper);
        c.noteAudition = Doc("NoteAudition", "공책 낙서",
            "방송부 오디션 떨어짐 ㅠㅠ\n\n서진이는 붙었다.\n목소리가 예뻐서 그렇대.\n\n…부럽다.", true, NotebookPaper);

        AssetDatabase.SaveAssets();
        return c;
    }

    static string AttendanceBody()
    {
        var lines = new System.Text.StringBuilder("<size=80%><color=#6B5B4B>2006학년도 · 담임 박영숙</color></size>\n\n");
        for (int i = 1; i <= 10; i++)
        {
            int right = i + 10;
            string rightName = Students[right - 1];
            if (right == 17) rightName += "  <size=75%><color=#8B1A1A>10/18~ 결석</color></size>";
            lines.Append($"<mspace=0.55em>{i,2}</mspace>   {Students[i - 1]}<pos=50%><mspace=0.55em>{right,2}</mspace>   {rightName}\n");
        }
        lines.Append("\n<size=80%><color=#6B5B4B>※ 자리는 칠판 옆 배치표를 따른다.</color></size>");
        return lines.ToString();
    }

    public static string SeatGrid()
    {
        var rows = new string[Seats.GetLength(0)];
        for (int r = 0; r < rows.Length; r++)
        {
            var cells = new string[Seats.GetLength(1)];
            for (int col = 0; col < cells.Length; col++) cells[col] = Seats[r, col].ToString().PadLeft(2);
            rows[r] = string.Join("    ", cells);
        }
        return string.Join("\n", rows);
    }

    static string SeatChartBody() =>
        "<align=center>[        칠        판        ]</align>\n\n" +
        $"<align=center><mspace=0.7em><line-height=140%>{SeatGrid()}</line-height></mspace></align>\n\n" +
        "<align=center><size=80%>← 복도 쪽                         창문 쪽 →</size></align>\n\n" +
        "<align=center><size=75%><color=#6B5B4B>(교실 뒤 · 사물함)</color></size></align>";

    static ItemData Item(string file, string displayName, string description)
    {
        string path = $"{ItemFolder}/{file}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemData>();
            AssetDatabase.CreateAsset(item, path);
        }
        item.displayName = displayName;
        item.description = description;
        EditorUtility.SetDirty(item);
        return item;
    }

    static DocumentData Doc(string file, string title, string body, bool handwritten, Color paper)
    {
        string path = $"{DocumentFolder}/{file}.asset";
        var doc = AssetDatabase.LoadAssetAtPath<DocumentData>(path);
        if (doc == null)
        {
            doc = ScriptableObject.CreateInstance<DocumentData>();
            AssetDatabase.CreateAsset(doc, path);
        }
        doc.title = title;
        doc.body = body;
        doc.handwritten = handwritten;
        doc.paperColor = paper;
        EditorUtility.SetDirty(doc);
        BuilderKit.UsedText.Append(title).Append(body);
        return doc;
    }
}
