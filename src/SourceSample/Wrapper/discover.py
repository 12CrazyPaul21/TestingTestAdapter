import sys
import subprocess

exe = sys.argv[1]

subprocess.run([exe, "--list-details"])
