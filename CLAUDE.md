# 01_Game_Project

Unity 6000.5.1f1 · URP (Universal 3D 템플릿) · Windows · 스크립트 에디터: Visual Studio 2026

## 파일 규칙

- 스크립트는 `Assets/Scripts/` 아래에 둔다. MonoBehaviour / ScriptableObject 클래스 이름은 파일 이름과 같아야 한다 (다르면 컴포넌트로 붙일 수 없다).
- `.cs` 파일은 UTF-8로 저장한다 (한글 주석 깨짐 방지, `.editorconfig`에 설정됨).
- `.meta` 파일은 직접 만들지 않는다. Unity가 자동 생성한다.
- 에셋을 옮기거나 이름을 바꿀 때는 `.meta`도 함께 옮긴다. GUID가 바뀌면 씬·프리팹 참조가 끊어진다. 가능하면 UnityMCP 도구로 에디터 안에서 처리한다.
- 자동 생성물은 읽거나 수정하지 않는다: `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx`.

## 씬 · 프리팹 · 에셋

- `.unity`, `.prefab`, `.asset`은 YAML 텍스트(Force Text)지만 직접 편집하지 않는다. GameObject·컴포넌트·씬 변경은 UnityMCP 도구로 한다.
- MCP가 연결되어 있지 않으면 에디터에서 할 작업을 사용자에게 단계별로 안내한다.
- 인스펙터에 연결된 `public` / `[SerializeField]` 필드 이름을 바꿀 때는 `[FormerlySerializedAs("이전이름")]`을 붙여 연결된 값을 유지한다.
- 씬을 `SceneManager.LoadScene("이름")`으로 불러오려면 Build Profiles의 씬 목록에 등록되어 있어야 한다.

## 코드

- 입력은 **Input System 패키지만** 사용한다 (Active Input Handling = Input System). `Input.GetAxis`, `Input.GetKeyDown` 같은 레거시 API는 런타임 예외가 난다. `InputSystem_Actions.inputactions` 또는 `Keyboard.current` / `Mouse.current`를 사용한다.
- Unity 6 API를 사용한다: `Rigidbody.linearVelocity` (`velocity`는 obsolete), `FindFirstObjectByType` / `FindObjectsByType` (`FindObjectOfType`는 obsolete).

## 검증

- Unity가 켜져 있으면 UnityMCP로 콘솔을 읽어 컴파일 에러·경고를 확인한다.
- Unity 없이 컴파일만 확인할 때: `dotnet build Assembly-CSharp.csproj -nologo -v q`
  - csproj는 Unity가 생성한다. 새로 만든 `.cs` 파일은 Unity가 csproj를 갱신한 뒤에야 포함된다.
