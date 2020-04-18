//var canvas = document.getElementById("line-chart");
//var context = canvas.getContext('2d');

//document.getElementById('clear').addEventListener('click', function () {
//    context.clearRect(0, 0, canvas.width, canvas.height);
//    createChart();
//}, false);

var startTime = null;
var startMessageNum = null;
var slice = 10;
var count = 0;
var index = 0;
var timeArray = new Array(slice);

var chart = new Chart(document.getElementById("line-chart"),
    {
        type: "scatter",
        data: {
            labels: [],
            datasets: [
                {
                    data: [],
                    label: "message/sec",
                    borderColor: "#3e95cd",
                    fill: false,
                    showLine: true
                }
            ]
        },
        options: {
            title: {
                display: true,
                text: "Throughput Chart"
            }
        }
    });

var connection = new signalR.HubConnectionBuilder().withUrl("/pisystemHub").withAutomaticReconnect().build();

connection.on("ReceiveMessage",
    function(message) {
        updateData(message);
        //var root = document.getElementById(id);
        //writeLine(root, message);
    });

connection.start();

//$.connection.hub.connectionSlow(function () {
//    //notifyUserOfConnectionProblem();
//    alert("Slow connection.");
//});

//$.connection.hub.reconnecting(function () {
//    //notifyUserOfTryingToReconnect();
//    alert("Reconnecting.");
//});

//$.connection.hub.disconnected(function () {
//    alert("Disconnected.");
//    //if (tryingToReconnect) {
//    //    notifyUserOfDisconnect(); // Your function to notify user.
//    //}
//});

//var tryingToReconnect = false;

//$.connection.hub.reconnecting(function () {
//    tryingToReconnect = true;
//});

//$.connection.hub.reconnected(function () {
//    tryingToReconnect = false;
//});

function setSize() {
    var num = document.getElementById("numMsgs").value;
    index = 0;
    slice = parseInt(num);
    timeArray = new Array(num);
}

function updateData(message) {
    var jsonObj = JSON.parse(message);

    timeArray[index] = (moment(jsonObj.lastMessageTimestamp));
    updateText(jsonObj.lastMessageTimestamp,
        jsonObj.messageCount,
        jsonObj.byteCount,
        jsonObj.lastErrorTimestamp,
        jsonObj.errorCount);
    index++;
    if (index === slice) {
        index = 0;
        d0 = Math.min.apply(Math, timeArray);
        d1 = Math.max.apply(Math, timeArray);
        console.log("array length " + timeArray.length);
        timeArray = new Array(slice);

        if (timeArray.length > 1) {
            dlast = moment(d1);
            dfirst = moment(d0);
            timeDiff = dlast.diff(dfirst);
            console.log("dlast " + dlast);
            console.log("dfirst " + dfirst);
            console.log("timeDiff " + timeDiff);
            timeDiff /= 1000;
            console.log("timeDiff adj " + timeDiff);

            rate = parseFloat(slice) / parseFloat(timeDiff);
            console.log("slice " + slice);
            console.log("rate" + rate);
            addData(d1, rate);
        } else {
            //d1 = Math.max.apply(Math, timeArray);
            //timeArray = new Array(slice);
            dlast = moment(d1);
            timeDiff = dlast / 1000;
            rate = parseFloat(slice) / timeDiff;
            addData(d1, rate);
        }
    }
}

function updateText(lastMessageTime, messageCount, byteCount, lastErrorTime, errorCount) {
    document.getElementById("lmt").textContent = lastMessageTime;
    document.getElementById("mc").textContent = messageCount;
    document.getElementById("bc").textContent = byteCount;

    document.getElementById("let").textContent = lastErrorTime;
    document.getElementById("ec").textContent = errorCount;
}

function addData(x1, y1) {
    chart.data.datasets[0].data.push({
        x: x1,
        y: y1
    });
    chart.update();
}

function subscribe(resourceUriString) {
    connection.invoke("SubscribeAsync", resourceUriString);
}

function toggleMonitor() {
    timeArray = new Array(slice);
    count = 0;
    index = 0;
    var parameters = new URLSearchParams(window.location.search);
    var resourceUriString = parameters.get("r");
    var span = document.getElementById("monitorAction");
    if (span.className === "glyphicon glyphicon-play") {
        span.className = "glyphicon glyphicon-stop";
        subscribe(resourceUriString);
    } else {
        span.className = "glyphicon glyphicon-play";
        connection.invoke("UnsubscribeAsync", resourceUriString);
    }
}

function goBack() {
    window.history.back();
}

function reload() {
    location.reload();
}