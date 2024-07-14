## Instalando as bibliotecas do VTK no sistema

para que o ProjetoContraste execute corretamente, as bibliotecas do VTK tem que ser instaladas globalmente no sistema, basta executar `make install-vtk` na raiz do projeto que as bibliotecas em `VTK-9.0.3-bin/lib` serão linkadas para `/usr/lib`

isso também tem que ser feito no lugar em que se pretende executar o `.so` do ProjetoContraste

sudo cp ../VTK-9.0.3-bin/lib/lib*.so* ../../opimage-processing-api/src/Api/Libs/vtkLibs/