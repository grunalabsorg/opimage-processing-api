import os
import subprocess
import SimpleITK as sitk

def mha_to_nifti(mha_file, nifti_file):
    try:
        print(f"\nConvertendo {mha_file} para NIfTI...")               
        image = sitk.ReadImage(mha_file)                
        sitk.WriteImage(image, nifti_file)
        print(f"Imagem {nifti_file} convertida para NIfTI")

    except Exception as e:
        print(f"Erro ao converter {mha_file} para NIfTI: {e}")

def nifti_to_dicom(nifti_file, dicom_output_dir, base_name, dicom_reference_file):
    try:
        print(f"\nConvertendo {nifti_file} para série DICOM em {dicom_output_dir} usando referência {dicom_reference_file}...")
        # Comando nifti2dicom via terminal
        command = [
            "nii2dcm", 
            f'"{nifti_file}"', 
            f'"{dicom_output_dir}"', 
            "-d", "MR", "-r", 
            f'"{dicom_reference_file}"'
        ]

        print("\nCommand: " + command)
        subprocess.run(command, check=True)
        print(f"Imagem {nifti_file} convertida para série DICOM em {dicom_output_dir}")
    except subprocess.CalledProcessError as e:
        print(f"Erro ao converter {nifti_file} para DICOM em {dicom_output_dir}: {e}")
    except Exception as e:
        print(f"Erro inesperado ao converter {nifti_file} para DICOM em {dicom_output_dir}: {e}")

def process_images(input_dir, dicom_reference_file):
    for file_name in os.listdir(input_dir):
        if file_name.endswith('.mha'):
            base_name = os.path.splitext(file_name)[0]
            mha_file = os.path.join(input_dir, file_name)
            nifti_file = os.path.join(input_dir, f"{base_name}.nii")
            dicom_output_dir = os.path.join(input_dir, base_name)

            if not os.path.exists(dicom_output_dir):
                os.makedirs(dicom_output_dir)

            mha_to_nifti(mha_file, nifti_file)
            nifti_to_dicom(nifti_file, dicom_output_dir, base_name, dicom_reference_file)

            try:
                a = 1
                #os.remove(nifti_file)
                #print(f"Arquivo temporário {nifti_file} removido")
            except Exception as e:
                print(f"Erro ao remover arquivo {nifti_file}: {e}")

if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Uso: python3 image_converter.py <diretorio_entrada> <diretorio_referencia_dicom>")
        print("argv[1]: "+sys.argv[1])
        print("argv[2]: "+sys.argv[2])                
    else:
        input_dir = sys.argv[1]
        dicom_reference_file = sys.argv[2]
        print(f"Iniciando conversão das imagens")
        process_images(input_dir, dicom_reference_file)
        print("Conversão concluída")

# python3 image_converter.py  "/home/aroldljs/opimage_tests/RESULTADO" "/home/aroldljs/opimage_tests/812459 AUREA EMILIA BEZERRA MADRUGA DE OLIVEIRA/1454914 RM MAMAS/MR 3D Ax VIBRANT ASPIR Mph C"
