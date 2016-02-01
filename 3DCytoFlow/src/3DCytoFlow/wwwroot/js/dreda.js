/*
 * DREDA
 *
 */

// globals
var scene, camera, renderer, controls, stats;
var canvas = document.getElementById('data-canvas');

// custom globals for data
var data;
var organized = [], objects = [];
var grouped;
var groupedSize;
var groupedKeys;

// color options (rainbow husl, 30 shades)
colors = _.shuffle(
["#f79987",
 "#f7818a",
 "#f85d8c",
 "#ed3694",
 "#da369d",
 "#cb36a4",
 "#bc36aa",
 "#ac36af",
 "#9a36b3",
 "#8136b9",
 "#5735bf",
 "#3764c0",
 "#3884be",
 "#3997bd",
 "#3aa6bc",
 "#3ab1bb",
 "#3bbcba",
 "#3cc8b8",
 "#3dd5b7",
 "#3ee6b4",
 "#59f6af",
 "#86f5a8",
 "#a3f5a0",
 "#bbf598",
 "#d2f58e",
 "#eaf580",
 "#f6e67b",
 "#f6d27f",
 "#f6bf82",
 "#f7ae85"]);

// ratios
var edge = 500;
var pointToEdgeRatio = .05;
var edgePaddingMultiplier = 1.3;
var lineToEdgeRatio = .002;
var lineGapToEdgeRatio = .002;
var lineDashToEdgeRatio = .006;

// camera ratios
var cameraYToEdgeRatio = 1.1;
var cameraZToEdgeRatio = 1.1;
var cameraXToEdgeRatio = -1.1;

// cluster selection
var raycaster = new THREE.Raycaster();
var selectionCube;
var mouse = new THREE.Vector2();
var initMouseDown;
var finalMouseDown;
var mouseDown = 0;
var selectionPlaneXY, selectionPlaneXZ, selectionPlaneYZ;

// calls
init();
animate();
resizeCamera();

////////////////
// functions //
////////////// 

function init() {

    // get canvas
    canvas.width = canvas.clientWidth;
    canvas.height = canvas.clientHeight;

    // create a new scene
    scene = new THREE.Scene();

    // add a camera
    var viewAngle = 45;
    var aspect = canvas.clientWidth / canvas.clientHeight;
    var near = 0.1;
    var far = edge * 20;
    camera = new THREE.PerspectiveCamera(viewAngle, aspect, near, far);
    scene.add(camera);

    // inital camera position is relativized to the overall scale
    camera.position.set(
      edge * cameraXToEdgeRatio,
      edge * cameraYToEdgeRatio,
      edge * cameraZToEdgeRatio);
    camera.lookAt(scene.position);

    // create the renderer
    // ReSharper disable once InconsistentNaming
    renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true, canvas: canvas });
    renderer.setViewport(0, 0, canvas.clientWidth - canvas.clientWidth / 4, canvas.clientHeight);
    renderer.setClearColor(0x1A1A1A, 1);

    // controls
    controls = new THREE.OrbitControls(camera, document, renderer.domElement);
    controls.autoRotate = true;

    // stats
    // TODO: Add Stats

    // add lights
    var light = new THREE.DirectionalLight(0xffffff, 1.5);
    light.position.set(100, 300, 100);
    scene.add(light);

    // add grid
    var gridxz = new THREE.GridHelper(edge, edge * .1);          //size, step
    gridxz.setColors(
      new THREE.Color(0x323232),                              //chill green
      new THREE.Color(0x323232)
    );
    gridxz.position.set(0, 0, 0);
    scene.add(gridxz);

    // add x axis line
    var lineGeometry = new THREE.Geometry();
    var vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(-edge, 0, 0), new THREE.Vector3(edge, 0, 0));
    lineGeometry.computeLineDistances();
    var lineMaterial = new THREE.LineDashedMaterial({ color: 0xf68870, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio, dashSize: lineDashToEdgeRatio });
    var line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    // add y axis line
    lineGeometry = new THREE.Geometry();
    vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(0, -edge, 0), new THREE.Vector3(0, edge, 0));
    lineGeometry.computeLineDistances();
    lineMaterial = new THREE.LineDashedMaterial({ color: 0x325cb1, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio, dashSize: lineDashToEdgeRatio });
    line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    // add z axis line
    lineGeometry = new THREE.Geometry();
    vertArray = lineGeometry.vertices;
    vertArray.push(new THREE.Vector3(0, 0, -edge), new THREE.Vector3(0, 0, edge));
    lineGeometry.computeLineDistances();
    lineMaterial = new THREE.LineDashedMaterial({ color: 0x3beca3, lineWidth: edge * lineToEdgeRatio, gapSize: lineGapToEdgeRatio, dashSize: lineDashToEdgeRatio });
    line = new THREE.Line(lineGeometry, lineMaterial);
    scene.add(line);

    // add the selection planes to be on the sides of the selection cube
    selectionPlaneXY = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'red', transparent: true, opacity: 0.5, side: THREE.DoubleSide }));
    selectionPlaneXZ = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'green', transparent: true, opacity: 0.5, side: THREE.DoubleSide }));
    selectionPlaneYZ = new THREE.Mesh(new THREE.PlaneGeometry(1, 1, 1, 1), new THREE.MeshBasicMaterial({ color: 'blue', transparent: true, opacity: 0.5, side: THREE.DoubleSide }));
    selectionPlaneXY.scale = 0.3;
    selectionPlaneXY.rotateX(0);
    selectionPlaneXZ.scale = 0.3;
    selectionPlaneXZ.rotateX(Math.PI / 2);
    selectionPlaneYZ.scale = 0.3;
    selectionPlaneYZ.rotateY(Math.PI / 2);
    scene.add(selectionPlaneXY);
    scene.add(selectionPlaneXZ);
    scene.add(selectionPlaneYZ);
    

    //objects.push(object);
    //scene.add(object);
}


function animate() {
    requestAnimationFrame(animate);
    render();
    update();
}

window.addEventListener("keydown", function (event) {
    var keyCode = event.keyCode;

    if (keyCode == 16) {
        selecting = true;
        controls.enabled = false;
    }
}, false);
window.addEventListener("keyup", function (event) {
    var keyCode = event.keyCode;

    // http://www.cambiaresearch.com/articles/15/javascript-char-codes-key-codes
    if (keyCode == 16) {
        selecting = false;
        controls.enabled = true;
    }
}, false);


function update() {
    controls.update();

    // UPDATE
    if (mouseDown == 1) {
        var sizeX = finalMouseDown.x - initMouseDown.x;
        var sizeY = finalMouseDown.y - initMouseDown.y;
        var sizeZ = finalMouseDown.z - initMouseDown.z;

        if (initMouseDown.normal.z == 1 || initMouseDown.normal.z == -1) {
            selectionPlaneXY.scale.x = Math.abs(sizeX);
            selectionPlaneXY.scale.y = Math.abs(sizeY);

            selectionPlaneXY.position.x = initMouseDown.x + sizeX / 2;
            selectionPlaneXY.position.y = initMouseDown.y + sizeY / 2;
            selectionPlaneXY.position.z = initMouseDown.z + 0.0001 * initMouseDown.normal.z;

            // Extra planes
            selectionPlaneYZ.scale.x = Math.abs(edge * 2);
            selectionPlaneYZ.scale.y = Math.abs(sizeY);

            selectionPlaneXZ.scale.x = Math.abs(sizeX);
            selectionPlaneXZ.scale.y = Math.abs(edge * 2);

            selectionPlaneYZ.position.y = selectionCube.position.y;
            selectionPlaneYZ.position.z = selectionCube.position.z;

            selectionPlaneXZ.position.x = selectionCube.position.x;
            selectionPlaneXZ.position.z = selectionCube.position.z;
        }
        else if (initMouseDown.normal.y == 1 || initMouseDown.normal.y == -1) {
            selectionPlaneXZ.scale.x = Math.abs(sizeX);
            selectionPlaneXZ.scale.y = Math.abs(sizeZ);

            selectionPlaneXZ.position.x = initMouseDown.x + sizeX / 2;
            selectionPlaneXZ.position.y = initMouseDown.y + 0.0001 * initMouseDown.normal.y;
            selectionPlaneXZ.position.z = initMouseDown.z + sizeZ / 2;

            // Extra planes
            selectionPlaneYZ.scale.x = Math.abs(sizeZ);
            selectionPlaneYZ.scale.y = Math.abs(edge * 2);

            selectionPlaneXY.scale.x = Math.abs(sizeX);
            selectionPlaneXY.scale.y = Math.abs(edge * 2);

            selectionPlaneXY.position.x = selectionCube.position.x;
            selectionPlaneXY.position.y = selectionCube.position.y;

            selectionPlaneYZ.position.y = selectionCube.position.y;
            selectionPlaneYZ.position.z = selectionCube.position.z;
        }
        else if (initMouseDown.normal.x == 1 || initMouseDown.normal.x == -1) {
            selectionPlaneYZ.scale.x = Math.abs(sizeZ);
            selectionPlaneYZ.scale.y = Math.abs(sizeY);

            selectionPlaneYZ.position.x = initMouseDown.x + 0.0001 * initMouseDown.normal.x;
            selectionPlaneYZ.position.y = initMouseDown.y = sizeZ / 2;
            selectionPlaneYZ.position.z = initMouseDown.z = sizeY / 2;

            // Extra planes
            selectionPlaneXY.scale.x = Math.abs(edge * 2);
            selectionPlaneXY.scale.y = Math.abs(sizeY);

            selectionPlaneXZ.scale.x = Math.abs(edge * 2);
            selectionPlaneXZ.scale.y = Math.abs(sizeZ);

            selectionPlaneXY.position.x = selectionCube.position.x;
            selectionPlaneXY.position.y = selectionCube.position.y;

            selectionPlaneXZ.position.x = selectionCube.position.x;
            selectionPlaneXZ.position.z = selectionCube.position.z;
        }
    }


    //stats.update();
}

function render() {
    renderer.render(scene, camera);
}

// resize - listener

window.addEventListener("resize", resizeCamera);

function resizeCamera()
{
    canvas.width = canvas.clientWidth;
    canvas.height = canvas.clientHeight;
    renderer.setViewport(0, 0, canvas.clientWidth, canvas.clientHeight);
    camera.aspect = canvas.clientWidth / canvas.clientHeight;
    camera.updateProjectionMatrix();
}

// mouse down
canvas.addEventListener("mousedown", function ( event ) 
{
    if (!(selecting === true)) return;

    // calculate mouse position in normalized device coordinates
    // (-1 to +1) for both components
    mouse.x = ( event.layerX / canvas.clientWidth) * 2 - 1;
    mouse.y = -(event.layerY / canvas.clientHeight) * 2 + 1;

    // update the picking ray with the camera and mouse position	
    raycaster.setFromCamera(mouse, camera);

    // calculate objects intersecting the picking ray
    var intersects = raycaster.intersectObjects([selectionCube], true);

    if (intersects.length > 0) {
        var normal;
        for (var i = 0; i < intersects.length; i++) {
            initMouseDown = intersects[i].point.clone();
            initMouseDown.normal = normal = intersects[i].face.normal;
            break;
        }

        mouseDown = 1;
    }
});

// mouse up
canvas.addEventListener("mousemove", function (event)
{
    if( mouseDown == 1 )
    {
        // calculate mouse position in normalized device coordinates
        // (-1 to +1) for both components
        mouse.x = ( event.layerX / canvas.clientWidth)*2 - 1;
        mouse.y = - ( event.layerY / canvas.clientHeight )*2 + 1;

        // update the picking ray with the camera and mouse position	
        raycaster.setFromCamera(mouse, camera);

        // calculate objects intersecting the picking ray
        var intersects = raycaster.intersectObjects([selectionCube]);

        if (intersects.length > 0) {
            var normal;
            for (var i = 0; i < intersects.length; i++) {
                finalMouseDown = intersects[i].point;
                break;
            }
        }
    }
});

// mouse up
window.addEventListener("mouseup", function (event)
{
    mouseDown = 0;

});



function rotateToggle() {
    controls.autoRotate = !controls.autoRotate;
}

function resetCamera() {
    camera.position.set(
      edge * cameraXToEdgeRatio,
      edge * cameraYToEdgeRatio,
      edge * cameraZToEdgeRatio);
    camera.lookAt(scene.position);
}

////////
////////
// DATA PROCESSING
///////
///////

function loadData() {

    // clear container
    organized = [];

    var dataLength = _.size(_.values(data)[0]);

    for (var i = 0; i < dataLength; i++) {
        var point = {};
        for (p in data) {
            if (data.hasOwnProperty(p)) {
                point[p] = data[p][i];
            }
        }
        organized.push(point);
    }
}

function plotData() {

    if (data == undefined) return false;

    // get those points

    loadData();

    // resize edge based on max min & reinitialize planes
    edge = getLimits() * edgePaddingMultiplier;
    init();

    var sprite = THREE.ImageUtils.loadTexture("../Images/disc.png");

    console.log("Initalized");
    // zip and group

    var keys = Object.keys(data);

    grouped = _.groupBy(organized, keys[3]); // group by color
    groupedSize = _.size(grouped);
    groupedKeys = Object.keys(grouped);

    for (var i = 0; i < groupedSize; i++) {

        // create new point cloud material
        var pointGeometry = new THREE.Geometry();
        var pointColors = [];

        // loop through points
        for (var j = 0; j < grouped[groupedKeys[i]].length; j++) {

            var vertex = new THREE.Vector3();
            vertex.x = grouped[groupedKeys[i]][j][keys[0]];
            vertex.y = grouped[groupedKeys[i]][j][keys[1]];
            vertex.z = grouped[groupedKeys[i]][j][keys[2]];
            pointGeometry.vertices.push(vertex);
            // assign colors
            pointColors[j] = new THREE.Color(colors[i]);
        }


        console.log(grouped[groupedKeys[i]].length + " points in cluster " + groupedKeys[i]);
        pointGeometry.colors = pointColors;

        // create new point cloud
        var pointCloudMaterial = new THREE.PointCloudMaterial({
            size: edge * pointToEdgeRatio,
            color: colors[i],
            vertexColors: THREE.VertexColors,
            alphaTest: 0.5,
            transparent: true,
            opacity: 0.8,
            map: sprite
        });

        // Use this to change the entire material 
        //pointCloudMaterial.color.setHSL( 3.0, 0.8, 0.8 );

        var pointCloud = new THREE.PointCloud(pointGeometry, pointCloudMaterial);
        scene.add(pointCloud);
        console.log(colors[i]);
    }

    //add cube
    //if graph gets messed in the future, add three.min V71
    selectionCube = cube(edge*2);

    //sgeometryCube.computeLineDistances();

    //var object = new THREE.LineSegments(geometryCube, new THREE.LineDashedMaterial({ color: 0xffaa00, dashSize: 3, gapSize: 1, linewidth: 2 }));

    //objects.push(object);
    scene.add(selectionCube);

    resizeCamera();

    return true;
}

function getLimits() {
    // container for potential x-z grid size
    var potentialEdges = [];
    for (col in data) {
        if (data.hasOwnProperty(col)) {
            // http://stackoverflow.com/questions/11142884
            var min = _.min(data[col]);
            var max = _.max(data[col]);
            if (isFinite(min) && isFinite(max)) {
                potentialEdges.push(Math.abs(min), max);
            }
        }
    }
    return _.max(potentialEdges);
}

function cube(size) {

    var cubeText = THREE.ImageUtils.loadTexture('images/threejsimages/cubeOutline.png');
    var cube = new THREE.Mesh(new THREE.CubeGeometry(size, size, size), new THREE.MeshBasicMaterial({map:cubeText, transparent:true, opacity:0.5}));

    /*geometry.vertices.push(
        new THREE.Vector3(-h, -h, -h),
        new THREE.Vector3(-h, h, -h),

        new THREE.Vector3(-h, h, -h),
        new THREE.Vector3(h, h, -h),

        new THREE.Vector3(h, h, -h),
        new THREE.Vector3(h, -h, -h),

        new THREE.Vector3(h, -h, -h),
        new THREE.Vector3(-h, -h, -h),


        new THREE.Vector3(-h, -h, h),
        new THREE.Vector3(-h, h, h),

        new THREE.Vector3(-h, h, h),
        new THREE.Vector3(h, h, h),

        new THREE.Vector3(h, h, h),
        new THREE.Vector3(h, -h, h),

        new THREE.Vector3(h, -h, h),
        new THREE.Vector3(-h, -h, h),

        new THREE.Vector3(-h, -h, -h),
        new THREE.Vector3(-h, -h, h),

        new THREE.Vector3(-h, h, -h),
        new THREE.Vector3(-h, h, h),

        new THREE.Vector3(h, h, -h),
        new THREE.Vector3(h, h, h),

        new THREE.Vector3(h, -h, -h),
        new THREE.Vector3(h, -h, h)
     );*/

    return cube;

}

