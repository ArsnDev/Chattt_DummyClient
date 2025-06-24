# DummyClient

Chattt 프로젝트용 더미 클라이언트(DummyClient)입니다.

## 소개

DummyClient는 Chattt 서버와의 통신을 테스트하거나, 개발 환경에서 클라이언트 역할을 대체하기 위해 제작된 간단한 C# 콘솔 애플리케이션입니다.

## 요구 사항

- .NET 8.0 SDK 이상
- Windows 10 이상

## 설치 및 실행

1. 저장소를 클론합니다.
   ```bash
   git clone [저장소 주소]
   cd DummyClient
   ```

2. 빌드합니다.
   ```bash
   dotnet build
   ```

3. 실행합니다.
   ```bash
   dotnet run --project DummyClient/DummyClient.csproj
   ```

## 사용 예시

실행 후 안내에 따라 서버 주소, 포트, 메시지 등을 입력하면 서버와의 통신 테스트가 가능합니다.

## 라이선스

MIT
