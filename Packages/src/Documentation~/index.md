# UnityOre

UnityOre는 OOJJRS Reporter 서버를 사용하는 게임용 Unity 클라이언트 패키지다.

## 간편 사용

`Assets/Resources/ReporterSettings.asset`에 서버 기본 주소, 프로젝트 키, 수집 토큰을 저장한다. 에셋은 Unity의 `Assets > Create > OOJJRS > Reporter Settings` 메뉴로 생성한다. 기본 주소에는 `/api/v1` 경로를 포함하지 않는다.

```csharp
await Ore.SendUxAsync($"GAME.START/{gameId}", cancellationToken);
await Ore.SendReportAsync(screenshot, "Failed to load profile", () => JsonUtility.ToJson(profile), cancellationToken);
```

`Ore`는 첫 전송 때 설정을 읽어 내부 클라이언트를 생성하고 이후 재사용하므로 별도 초기화 호출이 필요하지 않다. 사용자 정보가 있으면 기존 `WebReporter`처럼 `Ore.Id`, `Ore.Nickname`, `Ore.StoreType`에 지정할 수 있다.

간편 보고서 호출은 애플리케이션 정보와 사용자 정보, JPEG 스크린샷, `Player.log`의 마지막 2MB까지를 자동으로 구성한다. 스크린샷과 컨텍스트 함수는 필요하지 않으면 생략할 수 있다.

## 커스텀 보고서

`ReportRequest`의 `Summary`는 필수다. `ClientReportId`를 지정하면 서버가 중복 제출을 같은 보고서로 처리할 수 있다. `ContextJson`은 프로필이나 게임 상태처럼 호출 프로젝트가 직렬화한 JSON 값이다.

첨부파일은 `ReportAttachment`로 전달한다. 일반 바이트 첨부 외에 UTF-8 텍스트, JPEG, PNG 생성 함수를 제공한다. 서버의 첨부 개수와 크기 제한은 서버 설정을 따른다.

## 커스텀 이벤트

`EventRequest`의 `Name`은 필수다. `PropertiesJson`에는 이벤트별 속성을 JSON 값으로 전달한다.

```csharp
var request = new EventRequest("game.started")
{
    Application = ReportApplication.CreateUnity("Steam"),
    Message = "Campaign started",
    PropertiesJson = JsonUtility.ToJson(properties),
};
var options = new ReporterClientOptions("https://reporter.example.com", "my-game", ingestionToken);
var client = new ReporterClient(options);
var response = await client.SendEventAsync(request, cancellationToken);
```

## 응답과 오류

성공하면 `ReporterResponse`가 서버 문서 ID와 UTC 수신 시각을 제공한다. HTTP 오류와 네트워크 오류는 `ReporterException`으로 반환되며 `StatusCode`, `ResponseBody`, `IsNetworkError`를 확인할 수 있다. 취소는 `OperationCanceledException`으로 반환된다.

전송 함수는 Unity 메인 스레드에서 호출해야 하며 완료 continuation은 호출 시점의 Unity 동기화 컨텍스트를 따른다.
