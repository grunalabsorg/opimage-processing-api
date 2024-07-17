FROM mcr.microsoft.com/dotnet/sdk:6.0-jammy AS build
WORKDIR /app
COPY ["Api.csproj", "."]
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0-jammy AS runtime
RUN apt-get update && apt-get install -y curl python3 python3-pip
WORKDIR /app
COPY ["Libs/vtkLibs/libvtk*.so*", "/app"]
COPY ["Libs/libProjetoContraste.so", "/app"]
COPY ["OS/image_converter.py", "/app"]
USER root
RUN ln -sf /app/libvtk*.so* /usr/lib
COPY --from=build /app/publish .
RUN pip3 install SimpleITK nii2dcm
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Api.dll"]