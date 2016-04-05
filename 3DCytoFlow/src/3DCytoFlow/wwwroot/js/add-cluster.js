//saves selected cluster
function addCluster() {

    //if nothing is selected return
    if (selectionCube === undefined) return;

    //get the cluster name
    var clusterName = $("#clusterInput").val();

    //if the name is empty don't do anything else
    if (clusterName === undefined || clusterName === "") return;

    //display loading animation
    $("#loader").addClass("ui active dimmer");

    //get the id of the current analysis
    var analysisId = $("#deleteButton").attr("analysis");

    //remove hashtag from hex
    var color = window.selectedColor.split("#");

    //create js obj that represents the cluster
    var model = {
        Id: analysisId,
        Name: clusterName,
        Color: color[1],
        Depth: selectionCube.scale.x,
        Width: selectionCube.scale.y,
        Height: selectionCube.scale.z,
        X: selectionCube.position.x,
        Y: selectionCube.position.y,
        Z: selectionCube.position.z
    };

    //convert to json
    var jsonModel = JSON.stringify(model);

    //make the http request
    $.ajax({
        type: "GET",
        async: true,
        url: "/Clusters/SaveCluster?model=" + jsonModel
    }).done(function (response) {

        var json = JSON.parse(response);

        //the model needs the new received cluster id
        model.Id = json.Id;
        //if everything worked, remove the loading screen and add the cluster to the list of clusters
        window.currentClusters.push(model);
        
        $("#loader").removeClass("ui active dimmer");
        //   $('#clusterInfo').append('<div style="color:' + selectedColor + ';">' + clusterName + '</div>');
        $("#clusterInfo").append("<input class=\"clusterCheck\" style=\"margin-right: 10px;\" type=\"checkbox\" name=\"" + clusterName + "\" value=\"" + json.Id + "\" checked><span id=\"cluster" + json.Id + "\" style=\"color:" + window.selectedColor + ";\">" + clusterName + "</span></br>");

        var currentClusterNum = $("#clusterSize").text();
        $("#clusterSize").text(++currentClusterNum);

        //bind the on change event to new clusters
        $(".clusterCheck").each(function () {

            if (!$(this).is(":checked")) {
                $(this).prop("checked", true);
            }
        });
        $(".clusterCheck").change(function (e) {

            var clusterId = $(this).attr("value");

            window.selectDeselectClusters(clusterId, $(this));
        });

    }).fail(function () {
        //if failed, remove the loading screen and notify user
        $("#loader").removeClass("ui active dimmer");
        alert("failed to add cluster");
    });
}