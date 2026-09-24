# OOJJRS' Reporter Client

OOJJRS Reporter 서버 호출을 위한 Unity 패키지다.

## 주요 API

- `Ore.SendReport`: 스크린샷과 `Player.log`를 자동 첨부해 보고서를 전송한다.
- `Ore.SendMatchVerification`: 매치 검증 바이트 배열을 ZIP으로 압축해 전송한다.
- `Ore.SendUx`: UX 이벤트를 한 번의 호출로 전송한다.
- `ReporterClient.SendMatchVerification`: 매치 메타데이터와 검증 바이트 배열을 전송한다.
- `ReporterClient.SendReport`: 보고서 JSON과 첨부파일을 전송한다.
- `ReporterClient.SendEvent`: 구조화된 진단 및 UX 이벤트를 전송한다.
- `ReportAttachment`: 바이트, 텍스트, JPEG 또는 PNG 첨부파일을 구성한다.

## 사용

`Assets/Resources/ReporterSettings.asset`에 서버 주소, 프로젝트 키, 수집 토큰을 저장한다. 에셋은 Unity의 `Assets > Create > Ore > Reporter Settings` 메뉴로 생성한다.

```csharp
Ore.SendUx($"GAME.START/{gameId}", cancellationToken);
Ore.SendMatchVerification(matchId, matchVerificationBytes, cancellationToken: cancellationToken);
Ore.SendReport(screenshot, "Failed to load profile", () => JsonUtility.ToJson(profile), cancellationToken);
```

`Ore`는 첫 전송 때 설정을 읽어 클라이언트를 생성하고 이후 재사용한다. 별도 초기화 호출은 필요하지 않다. 사용자 정보가 있으면 기존 `WebReporter`처럼 `Ore.Id`, `Ore.Nickname`, `Ore.StoreType`에 지정할 수 있다.

간편 보고서 호출은 Unity 애플리케이션 정보와 사용자 정보, JPEG 스크린샷, 전체 `Player.log`를 자동으로 구성한다. 보고서 JSON과 첨부파일은 `report.zip` 하나로 압축해 전송하며, ZIP 전체 크기 제한은 서버 설정을 따른다. 스크린샷과 컨텍스트 함수는 필요하지 않으면 생략할 수 있다.

매치 검증 호출은 `matchId`와 게임 클라이언트가 생성한 바이트 배열을 받아 `match-verification.json`과 `attachments/match-verification.bin`을 포함하는 `match-verification.zip`을 구성한다. 클라이언트 제출 ID는 호출마다 자동 생성하며, 전체 ZIP 크기 제한은 서버 설정을 따른다.

직접 구성한 `EventRequest`, `MatchVerificationRequest`, `ReportRequest`, `ReportAttachment`를 받는 오버로드는 커스텀 전송에 사용한다. `BaseUrl`에는 `/api` 이전의 서버 주소를 지정한다. `ContextJson`과 `PropertiesJson`에는 JSON 값 하나를 문자열로 전달하며, 값이 비어 있거나 JSON `null`이면 빈 객체(`{}`)로 전송한다. 호출은 Unity 메인 스레드에서 시작해야 한다.

## 책임 범위

이 패키지는 서버 통신과 요청 데이터 구성을 담당하며 전송 실패는 Unity 로그에 기록한다. 게임별 신고 UI, 진행 표시, 사용자 알림, 프로필 직렬화는 소비 프로젝트가 담당한다.

클라이언트에 포함된 수집 토큰은 사용자가 추출할 수 있으므로 서버의 요청 제한과 토큰 교체 정책을 함께 운용해야 한다.
