# Changelog

## 1.1.2

- 이벤트, 보고서, 설정, 첨부파일의 `null` 문자열 입력을 빈 문자열로 처리하도록 수정했습니다.

## 1.1.1

- 전송 payload의 `null` 문자열을 빈 문자열로 정규화하도록 수정했습니다.

## 1.1.0

- 전송 API를 반환값 없는 `SendEvent`, `SendReport`, `SendUx`로 변경했습니다.
- `ReporterResponse`를 제거하고 전송 실패를 Unity 로그에 기록하도록 변경했습니다.

## 1.0.1

- ReporterSettings 생성 메뉴를 `OOJJRS`에서 `Ore`로 변경했습니다.

## 1.0.0

- Unity 프로젝트를 `Packages/src` 기반 UPM 패키지 구조로 전환했습니다.
- `com.oojjrs.ore` 패키지 메타데이터와 `oojjrs.ore` 런타임 어셈블리를 추가했습니다.
- Reporter 보고서와 이벤트를 전송하는 비동기 클라이언트를 추가했습니다.
- Unity 정보와 사용자 정보, 스크린샷, 로그를 자동 구성하는 간편 전송 API를 추가했습니다.
- 설정 에셋을 지연 로드하는 `Ore` 정적 전송 API를 추가했습니다.
- 텍스트와 이미지 첨부파일 구성 및 서버 오류 응답 모델을 추가했습니다.
