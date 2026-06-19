const connection = new signalR.HubConnectionBuilder()
    .withUrl("/subjectHub")
    .withAutomaticReconnect()
    .build();

connection.start()
    .then(() => {
        console.log("SubjectHub Connected");
    })
    .catch(err => console.error(err));

connection.on(
    "SubjectCreated",
    function (subjectId, subjectName) {

        alert("Môn học mới: " + subjectName);

        location.reload();
    });


connection.on(
    "SubjectUpdated",
    function (subjectId, subjectName) {

        alert("Môn học đã cập nhật: " + subjectName);

        location.reload();
    });

connection.on(
    "SubjectDeleted",
    function (subjectId) {

        alert("Môn học đã bị xóa");

        location.reload();
    });