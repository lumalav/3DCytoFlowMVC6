Senior Design Project in MVC6

**TODO:**

-Fix negative values in clusters

-Fix overwriting of analysis records when uploading data to the same patient.

-Add 404 handling and other errors.

-Refactor linux code (machinename, pswname must be stored in a file)

-~~crontab lock~~

-decide static/dynamic job number per machine

-lock analysis that has been already processed

-three.js and .json

-the run_pfromd.sh has to have this line at the end so the result file can be located
echo `pwd` > ../resultPath

-vm name and password hardcoded in the scripts

-remove the delta column in the analysis table and add color column in the cluster table

**Dependencies:**

Linux: 

[jq](https://stedolan.github.io/jq/)

[Azure CLI](https://azure.microsoft.com/en-us/documentation/articles/xplat-cli-install/)
