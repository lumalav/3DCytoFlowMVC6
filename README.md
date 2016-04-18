Senior Design Project in MVC6

**TODO:**

**> Priority** (BEFORE MONDAY)

~~-Unselecting clusters that have not been saved should return to original color~~

~~-toggle visibility for clusters~~

-Refactor linux code (machinename, pswname must be stored in a file)

-make sure the timestamp goes to zero when the analysis is done

-remove the delta column in the analysis table and add color column in the cluster table

-put a plane in the background to make the selection more user friendly

-add a button for changing the origin to the selected cluster every time a cluster is selected/when toggling the button

~~-implement dynamic job number per machine~~

~~-implement dynamic number of points per machine~~

~~-implement ETA~~

~~-put ETA(TIMESTAMP), jobNumber(INT32), machineNumber(INT32) in the database~~

**< Priority**

-Add 404 handling and other errors.

-design undo and redo

-implement undo redo

-found a bug in the visualizer. Make a cluster visible/nonvisible. Zoom in closer, press shift and drag the mouse. The cluster will change to the selected color

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
