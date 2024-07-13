# See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Api/Api.csproj", "src/Api/"]
RUN dotnet restore "src/Api/Api.csproj"
COPY . .
WORKDIR "/src/src/Api"
RUN dotnet build "Api.csproj" -c Release -o /app/build

# COPY libProjetoContraste to app folder
COPY ["src/Api/Libs", "/app"]

# vtk libs
RUN mkdir -p /usr/lib/ && \
    cp /usr/lib/libvtk*.so* /usr/lib/

FROM build AS publish
RUN dotnet publish "Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Api.dll"]

#FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
#WORKDIR /app
#
#COPY *.csproj ./
#RUN dotnet restore
#
#COPY . .
#
#RUN dotnet publish -c Release -o out
#
#FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime
#WORKDIR /app
#COPY --from=build /app/out ./
#
#EXPOSE 80
#ENTRYPOINT ["dotnet", "Api.dll"]