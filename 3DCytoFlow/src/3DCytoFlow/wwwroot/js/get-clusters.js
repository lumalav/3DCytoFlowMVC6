//gets all the clusters of the current analysis
function getClusters() {
    //get the id of the current analysis
    var analysisId = $("#deleteButton").attr("analysis");

    //if there is no id return
    if (analysisId === undefined || analysisId === "") return;

    //http request
    $.ajax({
        type: "GET",
        async: true,
        url: "/Clusters/GetClusters?id=" + analysisId
    }).done(function (model) {

        if (model !== undefined) {
            //if everything worked, add the clusters to the list of clusters
            var json = JSON.parse(model);

            $("#clusterSize").text(json.length);
            for (var i = 0; i < json.length; i++) {

                var cluster = json[i];

                window.currentClusters.push(cluster);

                var split = cluster.Name.split("#");

                var color = "#" + split[1];

                var height = cluster.Height;
                var depth = cluster.Depth;
                var width = cluster.Width;
                var x = cluster.X;
                var y = cluster.Y;
                var z = cluster.Z;

                window.selectionCube.scale.x = depth;
                window.selectionCube.scale.y = width;
                window.selectionCube.scale.z = height;
                window.selectionCube.position.x = x;
                window.selectionCube.position.y = y;
                window.selectionCube.position.z = z;

                window.colorSelectedPoints(color, 0);

                $("#clusterInfo").append("<input id=\"cluster" + cluster.Id + "\" class=\"clusterCheck\" style=\"margin-right: 10px;\" type=\"checkbox\" name=\"" + split[0] + "\" value=\"" + cluster.Id + "\"><span style=\"color:" + color.toUpperCase() + ";\">" + split[0] + "</span></br>");
            }

            //clean selectionCube coordinates
            window.selectionCube = new THREE.Mesh(new THREE.BoxGeometry(1, 1, 1, 20, 20, 20), new THREE.MeshBasicMaterial({ color: 'red', opacity: 0.8 }));

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

            //remove the loading animation
            $("#loader").removeClass("ui active dimmer");
        }

    }).fail(function () {
        //if failed, remove the loading screen and notify user
        $("#loader").removeClass("ui active dimmer");
        alert("failed to retrieve clusters");
    });
}