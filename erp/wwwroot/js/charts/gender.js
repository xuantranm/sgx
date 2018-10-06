$(function () {
    //

    var config = {
        type: 'pie',
        data: {
            datasets: [{
                data: [
                    60,
                    40
                ],
                backgroundColor: [
                    'brown',
                    'green'
                ],
                label: 'Giới tính'
            }],
            labels: [
                'Nam',
                'Nữ'
            ]
        },
        options: {
            responsive: true
        }
    };

    var genderChart = document.getElementById('genderChart').getContext('2d');
    var chart = new Chart(genderChart, config);
});