//Removes a selected cluster
function removeCluster() {

    //array to keep track of the checkboxes and their clusters
    var clustersToBeDeleted = [];
    var checkBoxesToBeRemoved = [];

    //push in the arrays those that are selected
    $(".clusterCheck").each(function (i) {

        if ($(this).is(":checked")) {
            var id = $(this).val();
            clustersToBeDeleted.push({ Id: id });

            var checkBox = $(this);
            checkBoxesToBeRemoved.push(checkBox);          
        }
    });

    //if the array is empty dont do anything
    if (clustersToBeDeleted.length < 1) return;

    //display loading animation
    $("#loader").addClass("ui active dimmer");

    //generate json
    var model = JSON.stringify(clustersToBeDeleted);

    //http request
    $.ajax({
        type: "POST",
        async: true,
        url: "/Clusters/RemoveCluster?model=" + model
    }).done(function (model) {

        //remove ui elements and repaint points
        for (var j = 0; j < checkBoxesToBeRemoved.length; j++) {
            $("#cluster" + clustersToBeDeleted[j].Id).next().detach();
            $("#cluster" + clustersToBeDeleted[j].Id).remove();
            var removed = false;
            var k;

            for (k = 0; k < window.currentClusters.length; k++) {

                if (window.currentClusters[k].Id === parseInt(clustersToBeDeleted[j].Id, 10)) {
                    window.selectionCube.scale.x = window.currentClusters[k].Depth;
                    window.selectionCube.scale.y = window.currentClusters[k].Width;
                    window.selectionCube.scale.z = window.currentClusters[k].Height;
                    window.selectionCube.position.x = window.currentClusters[k].X;
                    window.selectionCube.position.y = window.currentClusters[k].Y;
                    window.selectionCube.position.z = window.currentClusters[k].Z;

                    window.colorSelectedPoints("#ffffff", 0);

                    removed = true;
                    break;
                }
            }

            if (removed) window.currentClusters.splice(k, 1);           
        }

        for (var i = 0; i < checkBoxesToBeRemoved.length; i++) {
            $(checkBoxesToBeRemoved[i]).detach();
        }

        //update info
        var currentClusterNum = $("#clusterSize").text();
        $("#clusterSize").text(currentClusterNum - checkBoxesToBeRemoved.length);

        //if everything worked, remove the loading screen and remove the cluster from the list of clusters
        $("#loader").removeClass("ui active dimmer");

    }).fail(function () {
        //if failed, remove the loading screen and notify user
        $("#loader").removeClass("ui active dimmer");
        alert("failed to remove cluster");
    });
}