import os
import shutil

bufferDir = "buffers"
outputDir = "../../../../../../../../Bin/Debug"
destinationDir = "AppData/acs/buffers"

bufferPath = os.path.join(os.getcwd(), bufferDir)
if not os.path.exists(bufferPath) or not os.path.isdir(bufferPath):
    print("Failed to locate buffers directory")
    exit()

outputPath = os.path.join(os.getcwd(), outputDir)
if not os.path.exists(outputPath) or not os.path.isdir(outputPath):
    print("Failed to locate output directory")
    exit()

outputPath = os.path.join(outputPath, destinationDir)
if os.path.exists(outputPath):
    shutil.rmtree(outputPath)

try:
    # os.makedirs(outputPath)
    shutil.copytree(bufferPath, outputPath)
except Exception as ex:
    print("File copy exception.")
    print(ex)
