//alarm
var alarm1 = document.getElementsByName("Alarm.AlarmStatus1");
var highAL1 = document.getElementById("Alarm_HighAlarmTemp");
var lowhAL1 = document.getElementById("Alarm_LowAlarmTemp");
var alarm2 = document.getElementsByName("Alarm.AlarmStatus2");
var highAL2 = document.getElementById("Alarm_HighAlarmHumid");
var lowhAL2 = document.getElementById("Alarm_LowAlarmHumid");
for (var i = 0, max = alarm1.length; i < max; i++) {
    alarm1[i].onclick = function () {
        if (this.value=="True") {
            highAL1.disabled = false;
            lowhAL1.disabled = false;
            //highAL1.value = "";
            //lowhAL1.value = "";
        } else {
            highAL1.disabled = true;
            lowhAL1.disabled = true;
            highAL1.value = "1000";
            lowhAL1.value = "1000";
        }
    }
    alarm2[i].onclick = function () {
        if (this.value == "True") {
            highAL2.disabled = false;
            lowhAL2.disabled = false;
            //highAL1.value = "";
            //lowhAL1.value = "";
        } else {
            highAL2.disabled = true;
            lowhAL2.disabled = true;
            highAL2.value = "1000";
            lowhAL2.value = "1000";
        }
    }
}


//interval,duration
var txtDuraDay = document.getElementById("DurationDay");
var txtDuraHour = document.getElementById("DurationHour");
var txtInterSec = document.getElementById("IntervalSec");
var txtInterMin = document.getElementById("IntervalMin");
var txtInterHour = document.getElementById("IntervalHour"); 
//interval Send lora
var txtIntervalSendLoraDay = document.getElementById("IntervalSendLoraDay");
var txtIntervalSendLoraHour = document.getElementById("IntervalSendLoraHour");
var txtIntervalSendLoraMin = document.getElementById("IntervalSendLoraMin");
var changedDuration = false;
var changedInterval = true;
var durationMax = 365 * 24 * 3600;
var memory = 32400;
var intervalMax = DurationToInterval(durationMax);
txtDuraDay.onclick =
    function ChangedInterval() {
        changedDuration = true;
        changedInterval = false;
    }
txtDuraHour.onclick =
    function ChangedInterval() {
        changedDuration = true;
        changedInterval = false;
    }
txtInterHour.onclick =
    function () {

        changedDuration = false;
        changedInterval = true;
    };
txtInterMin.onclick =
    function () {
        changedDuration = false;
        changedInterval = true;
    };
txtInterSec.onclick =
    function () {
        changedDuration = false;
        changedInterval = true;
        console.log("veo");
    };
function calculateInterval() {
    if (changedDuration == false) {
        return;
    }
    var duraDay = 0;
    var duraHour = 0;
    try {
        duraDay = parseInt(txtDuraDay.value);
    }
    catch
    {
        changedDuration = false;
        duraDay = 0;
        txtDuraDay.value = 0;
    }

    try {
        duraHour = parseInt(txtDuraHour.value);
    }
    catch
    {
        changedDuration = false;
        duraHour = 0;
        txtDuraHour.value = 0;

    }

    var duration = parseInt((duraDay * 24 + duraHour) * 3600);


    var interval = DurationToInterval(duration);


    if (interval < 2) {
        interval = 2;
    }
    if (interval > intervalMax) {
        interval = intervalMax;
    }
    var intervalHour = parseInt(interval / 3600);
    var intervalMin = parseInt((interval % 3600) / 60);
    var intervalSec = parseInt((interval % 3600) % 60);

    txtInterHour.value = intervalHour;
    txtInterMin.value = intervalMin;
    txtInterSec.value = intervalSec;
    var duration2 = IntervalToDuration(interval);
    if (duration2 != duration) {
        duraDay = parseInt(duration2 / (24 * 60 * 60));
        duraHour = parseInt((duration2 % (24 * 60 * 60)) / 3600);

        if (duraDay >= 365) {
            duraHour = 0;
            duraDay = 365;
        }
        changedDuration = false;
        txtDuraDay.value = duraDay;
        txtDuraHour.value = duraHour;
    }
    SetMinMaxValueForIntervalLora(interval);
}
function SetMinMaxValueForIntervalLora(interval) {
    var loraInterval = 60 * interval;
    txtIntervalSendLoraMin.attributes.max.value = ((loraInterval % 3600) / 60).toString();
    txtIntervalSendLoraHour.attributes.max.value = ((loraInterval / 3600)).toString();
    txtIntervalSendLoraDay.attributes.max.value = ((loraInterval / 86400)).toString();
    var intervalHour = parseInt(interval / 3600);
    var intervalMin = parseInt((interval % 3600) / 60);
    var intervalSec = parseInt((interval % 3600) % 60);
    txtIntervalSendLoraMin.attributes.min.value = intervalMin;
    if (intervalSec > 0) {
        txtIntervalSendLoraMin.attributes.min.value = intervalMin + 1;
    }
    txtIntervalSendLoraHour.attributes.min.value = intervalHour;
}
function DurationToInterval(duration) {
    var value = duration % memory;
    if (value == 0)
        return parseInt(duration / memory);
    else
        return parseInt(duration / memory + 1);
}

function IntervalToDuration(interval) {
    return parseInt(memory * interval);
}


function calculateDuration() {
    if (changedInterval == false) {
        return;
    }
    var interHour = 0;
    var interMin = 0;
    var interSec = 0;
    try {
        interHour = parseInt(txtInterHour.value);
    }
    catch
    {
        interHour = 0;
        txtInterHour.value = 0;
    }

    try {
        interMin = parseInt(txtInterMin.value);
    }
    catch
    {
        interMin = 0;
        txtInterMin.value = 0;
    }

    try {
        interSec = parseInt(txtInterSec.value);
    }
    catch
    {
        interSec = 2;
        txtInterSec.value = 2;
    }

    var interval = parseInt(interHour * 3600 + interMin * 60 + interSec);

    if (interval > intervalMax) {
        interval = intervalMax;
        txtInterHour.value = parseInt(interval / 3600);
        txtInterMin.value = parseInt((interval % 3600) / 60);
        txtInterSec.value = parseInt((interval % 3600) % 60);
    }
    if (interval < 2) {
        interval = 2;
        txtInterHour.value = 0;
        txtInterMin.value = 0;
        txtInterSec.value = 2;
    }
    var duration = IntervalToDuration(interval);

    if (duration > durationMax) {
        duration = durationMax;
    }
    var duraDay = parseInt(duration / (24 * 60 * 60));
    var duraHour = parseInt((duration % (24 * 60 * 60)) / 3600);



    txtDuraDay.value = duraDay;
    txtDuraHour.value = duraHour;

    var interval2 = DurationToInterval(duration);
    if (interval2 != interval) {
        txtInterHour.value = parseInt(interval / 3600);
        txtInterMin.value = parseInt((interval % 3600) / 60);
        txtInterSec.value = parseInt((interval % 3600) % 60);
    }
    SetMinMaxValueForIntervalLora(interval);
    
}

txtInterSec.onchange = function () {
    calculateDuration();
    changedInterval = false;
}
txtInterMin.onchange = function () {
    calculateDuration();
    changedInterval = false;
}
txtInterHour.onchange = function () {
    calculateDuration();
    changedInterval = false;
}
txtDuraDay.onchange = function () {
    calculateInterval();
    changedDuration = false;
}
txtDuraHour.onchange = function () {
    calculateInterval();
    changedDuration = false;
}
//// Install input filters.
//setInputFilter(document.getElementById("txtIntervalSec"), function (value) {
//    return /^\d*$/.test(value);
//});
//// Restricts input for the given textbox to the given inputFilter.
//function setInputFilter(textbox, inputFilter) {
//    ["input", "keydown", "keyup", "mousedown", "mouseup", "select", "contextmenu", "drop"].forEach(function (event) {
//        if (textbox != null) {
//            textbox.addEventListener(event, function () {
//                if (inputFilter(this.value)) {
//                    this.oldValue = this.value;
//                    this.oldSelectionStart = this.selectionStart;
//                    this.oldSelectionEnd = this.selectionEnd;
//                } else if (this.hasOwnProperty("oldValue")) {
//                    this.value = this.oldValue;
//                    this.setSelectionRange(this.oldSelectionStart, this.oldSelectionEnd);
//                } else {
//                    this.value = "";
//                }
//            });
//        }
//    });
//}

//var btnSave = document.getElementById("btnSave");
//btnSave.onclick = function () {
//    if (txtInterSec.value == null) {
//        alert("Please Fill All Required Field");
//    }
//}


