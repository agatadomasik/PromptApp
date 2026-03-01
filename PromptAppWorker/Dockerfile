FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY PromptAppWorker/PromptAppWorker.csproj ./PromptAppWorker/
COPY PromptApp/PromptApp.csproj ./PromptApp/

RUN dotnet restore PromptAppWorker/PromptAppWorker.csproj

COPY . .

RUN dotnet publish PromptAppWorker/PromptAppWorker.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "PromptAppWorker.dll"]