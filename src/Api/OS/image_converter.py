import SimpleITK as sitk
import pydicom
import os
import time

def extract_base_name(full_path):
    filename_with_extension = os.path.basename(full_path)
    filename_without_extension = os.path.splitext(filename_with_extension)[0]
    return filename_without_extension

def mha_to_dcm(mha_filename, ref_dcm_filename):
    base_name = extract_base_name(mha_filename)    
    output_dcm_dir = os.path.join(os.path.dirname(mha_filename), base_name)    
    print(f"\nConverting {base_name} in {output_dcm_dir} ...")

    image = sitk.ReadImage(mha_filename)
    reference_image = sitk.ReadImage(ref_dcm_filename)

    if not os.path.exists(output_dcm_dir):
        os.makedirs(output_dcm_dir)

    tags_to_copy = [
        "0010|0010",  # Patient Name
        "0010|0020",  # Patient ID
        "0010|0030",  # Patient Birth Date
        "0010|0040",  # Patient's Sex
        "0008|0090",  # Referring Physician's Name
        "0008|0080",  # Institution Name
        "0008|0070",  # Manufacturer
        "0008|1090",  # Manufacturer's Model Name
        "0008|1010",  # Station Name
        "0020|000d",  # StudyUid
        "0020|0010",  # StudyId
        "0008|0020",  # Study Date
        "0008|0030",  # Study Time
        "0008|0050",  # Accession Number
        "0008|1030",  # Study Description
        "0008|0060",  # Modality

        "0008|0008",  # Image Type
        "0028|0030",  # Pixel Spacing
        "0018|0050",  # Slice Thickness
        "0018|1310",  # Acquisition Matrix
        # "0054|0081",  # Number of Slices
        "0028|1050",  # Window Center
        "0028|1051",  # Window Width
        "0018|0088",  # Spacing Between Slices
    ]

    direction = image.GetDirection()
    spacing = image.GetSpacing()
    origin = image.GetOrigin()

    num_slices = image.GetDepth()

    writer = sitk.ImageFileWriter()

    writer.KeepOriginalImageUIDOn()

    modification_time = time.strftime("%H%M%S")
    modification_date = time.strftime("%Y%m%d")

    ds = pydicom.dataset.Dataset()
    series_uid = pydicom.uid.generate_uid()

    for i in range(num_slices):
        image_slice = image[:,:,i]

        for tag in tags_to_copy:
            image_slice.SetMetaData(tag, reference_image.GetMetaData(tag))

        image_slice.SetMetaData("0008|103e", base_name+" (MR_ANALYSIS)")                                        # Series Description
        image_slice.SetMetaData("0008|0012", time.strftime("%Y%m%d"))                                           # Instance Creation Date
        image_slice.SetMetaData("0008|0013", time.strftime("%H%M%S"))                                           # Instance Creation Time
        image_slice.SetMetaData("0020|0037", '\\'.join(map(str, (direction[0], direction[3], direction[6],
        direction[1],direction[4],direction[7])))),                                                             # Image Orientation (Patient)
        image_slice.SetMetaData("0020|0032", '\\'.join(map(str,image.TransformIndexToPhysicalPoint((0,0,i)))))  # Image Position (Patient)
        image_slice.SetMetaData("0020|0013", str(i))                                                            # Instance Number                
        image_slice.SetMetaData("0020|000e", series_uid)                                                        # Series Uid

        sop_uid = pydicom.uid.generate_uid()
        image_slice.SetMetaData("0002|0003", sop_uid)                                                           # Media Storage SOP Instance UID
        image_slice.SetMetaData("0008|0018", sop_uid)                                                           # SOP Instance UID

        image_slice.SetMetaData("Generated image","")

        # Write to the output directory and add the extension dcm, to force writing in DICOM format.
        #writer.SetFileName(os.path.join(output_dcm_dir,f'{i:03d}.dcm'))
        writer.SetFileName(os.path.join(output_dcm_dir,f'{sop_uid}.dcm'))
        writer.Execute(image_slice)
    
    print('Successful convert')

def convert_images_in_dir(directory, ref_dcm_filename):
    for file_name in os.listdir(directory):
        if file_name.endswith('.mha'):          
            mha_filename = os.path.join(directory, file_name)           
            mha_to_dcm(mha_filename, ref_dcm_filename)
            try:                 
                os.remove(mha_filename)              
            except Exception as e:
                print(f"\nError in remove {mha_filename}: {e}")

    print('\nAll files was converted')
    
if __name__ == "__main__":
    import sys
    if len(sys.argv) != 3:
        print("Use: python3 image_converter.py <input_dir> <dicom_reference_file>")
        print("argv[1]: "+sys.argv[1])
        print("argv[2]: "+sys.argv[2])                        
    else:
        directory = sys.argv[1]
        ref_dcm_filename = sys.argv[2]        
        convert_images_in_dir(directory, ref_dcm_filename)
