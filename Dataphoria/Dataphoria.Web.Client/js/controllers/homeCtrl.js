controllers.controller('HomeCtrl', ['$scope', 'dataService', function ($scope, dataService) {
	$scope.error = "";
	
	loadData();
	function loadData() {
		dataService.ToDoList.query(function success(data) {
			$scope.rows = data.Data;
		});
	}

	$scope.delete = function (row) {
		dataService.ToDoList.delete({ id: row.Id }, function success(data) {
			$scope.rows.splice($scope.rows.indexOf(row), 1);
		},
		function error(err) {

		});
	};

	$scope.new = function () {
		$scope.newRow = {};
		$scope.isAdding = true;
	};

	$scope.save = function (row) {
		row.Id = parseInt(row.Id);
		dataService.ToDoList.save({}, row, function success(data) {
			$scope.rows.push(row);
		},
		function error(err) {

		});

		$scope.isAdding = false;
	};
}]);