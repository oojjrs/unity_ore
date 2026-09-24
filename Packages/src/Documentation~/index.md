# UnityOre

UnityOre는 OOJJRS Reporter 서버를 사용하는 게임용 Unity 클라이언트 패키지다.

## 간편 사용

`Assets/Resources/ReporterSettings.asset`에 서버 기본 주소, 프로젝트 키, 수집 토큰을 저장한다. 에셋은 Unity의 `Assets > Create > Ore > Reporter Settings` 메뉴로 생성한다. 기본 주소에는 `/api/v1` 경로를 포함하지 않는다.

```csharp
Ore.SendUx($"GAME.START/{gameId}", cancellationToken);
Ore.SendMatchVerification(matchId, matchVerificationBytes, cancellationToken: cancellationToken);
Ore.SendReport(screenshot, "Failed to load profile", cancellationToken: cancellationToken);
```

`Ore`는 첫 전송 때 설정을 읽어 내부 클라이언트를 생성하고 이후 재사용하므로 별도 초기화 호출이 필요하지 않다. 사용자 정보가 있으면 기존 `WebReporter`처럼 `Ore.Id`, `Ore.Nickname`, `Ore.StoreType`에 지정할 수 있다.

간편 보고서 호출은 JPEG 스크린샷과 전체 `Player.log`를 자동으로 구성한다. 첨부파일은 별도 폴더 없이 `report.zip` 루트에 압축해 전송하며, ZIP 전체 크기 제한은 서버 설정을 따른다. 스크린샷은 필요하지 않으면 생략할 수 있다.

## 커스텀 보고서

`ReportRequest`를 받는 오버로드는 기존 호출 호환성을 유지하며 보고서 메타데이터를 전송하지 않는다.

첨부파일은 `ReportAttachment`로 전달한다. 일반 바이트 첨부 외에 UTF-8 텍스트, JPEG, PNG 생성 함수를 제공한다. 클라이언트는 첨부파일을 `report.zip` 루트에 바로 구성하며, 전체 ZIP 크기 제한은 서버 설정을 따른다.

## 매치 검증 데이터

`Ore.SendMatchVerification`은 `matchId`와 게임 클라이언트가 만든 바이트 배열을 받는다. 바이트 배열은 `attachments/match-verification.bin`으로 압축하고, 애플리케이션·사용자·클라이언트 제출 ID를 담은 `match-verification.json`을 ZIP 루트에 추가한 뒤 `match-verification.zip` 하나로 전송한다. 구조화된 검증 컨텍스트가 필요하면 JSON 생성 함수를 함께 전달할 수 있다.

고정된 클라이언트 제출 ID나 발생 시각을 직접 지정해야 할 때는 `MatchVerificationRequest`를 구성해 `ReporterClient.SendMatchVerification`에 전달한다. 전체 ZIP 크기 제한은 서버 설정을 따른다.

## 커스텀 이벤트

`EventRequest`의 `Name`은 필수다. `PropertiesJson`에는 이벤트별 속성을 JSON 값으로 전달하며, 비어 있거나 JSON `null`이면 빈 객체(`{}`)로 전송한다.

```csharp
var request = new EventRequest("game.started")
{
    Application = ReportApplication.CreateUnity("Steam"),
    Message = "Campaign started",
    PropertiesJson = JsonUtility.ToJson(properties),
};
var options = new ReporterClientOptions("https://reporter.example.com", "my-game", ingestionToken);
var client = new ReporterClient(options);
client.SendEvent(request, cancellationToken);
```

## 전송과 오류

전송 함수는 요청을 시작한 뒤 반환값 없이 즉시 종료한다. HTTP 오류와 네트워크 오류는 Unity 로그에 기록하며 취소는 별도 오류로 기록하지 않는다.

전송 함수는 Unity 메인 스레드에서 호출해야 한다.
