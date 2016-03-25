Senior Design Project in MVC6

**TODO:**

**> Priority**

-Unselecting clusters that have not been saved should return to original color

**< Priority**

-Add 404 handling and other errors.

-Refactor linux code (machinename, pswname must be stored in a file)

-implement dynamic job number per machine

-vm name and password hardcoded in the scripts

-remove the delta column in the analysis table and add color column in the cluster table

-put a plane in the background to make the selection more user friendly

**Fixed**

-~~Fix synchronization problem between clusters and graph~~

-~~lock analysis that has been already processed~~

-~~Fix negative values in clusters~~

-~~Fix overwriting of analysis records when uploading data to the same patient~~

-~~crontab lock~~

-~~three.js and .json together~~

**Dependencies:**

-the run_pfromd.sh has to have this line at the end so the result file can be located
echo `pwd` > ../resultPath

Linux: 

[jq](https://stedolan.github.io/jq/)

[Azure CLI](https://azure.microsoft.com/en-us/documentation/articles/xplat-cli-install/)
