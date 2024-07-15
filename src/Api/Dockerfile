FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy AS build
WORKDIR /app
COPY ["Api.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0-jammy AS runtime
RUN apt-get update && apt-get install -y curl
WORKDIR /app
COPY ["Libs/vtkLibs/libvtk*.so*", "/app"]
COPY ["Libs/libProjetoContraste.so", "/app"]
USER root
RUN ln -sf /app/libvtk*.so* /usr/lib
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
COPY --from=build app/publish .
ENTRYPOINT ["dotnet","Api.dll"]