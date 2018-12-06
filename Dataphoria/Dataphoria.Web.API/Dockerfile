FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["../Dataphoria/Dataphoria.Web.API/Dataphoria.Web.API.csproj", "../Dataphoria/Dataphoria.Web.API/"]
COPY ["../Libraries/PGSQLConnection/Source/DAE.Connection.PGSQL.csproj", "../Libraries/PGSQLConnection/Source/"]
COPY ["Base/Base.csproj", "Base/"]
COPY ["Server/Server.csproj", "Server/"]
COPY ["Base.Windows/Base.Windows.csproj", "Base.Windows/"]
COPY ["DAE/Engine.csproj", "DAE/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["DAE.NativeCLI/DAE.NativeCLI.csproj", "DAE.NativeCLI/"]
COPY ["DAE.PGSQL/DAE.PGSQL.csproj", "DAE.PGSQL/"]
COPY ["../Libraries/Connection/Source/DAE.Connection.csproj", "../Libraries/Connection/Source/"]
COPY ["DAE.SQLite/DAE.SQLite.csproj", "DAE.SQLite/"]
RUN dotnet restore "../Dataphoria/Dataphoria.Web.API/Dataphoria.Web.API.csproj"
COPY . .
WORKDIR "/src/../Dataphoria/Dataphoria.Web.API"
RUN dotnet build "Dataphoria.Web.API.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "Dataphoria.Web.API.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Dataphoria.Web.API.dll"]