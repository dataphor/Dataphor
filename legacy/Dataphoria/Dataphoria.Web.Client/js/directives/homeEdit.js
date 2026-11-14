directives.directive('homeEdit', [function () {
	return {
		restrict: 'E',
		replace: false,
		templateUrl: 'partials/home/edit.html',
		scope: {
			row: '='
		},
		controller: ['$scope', 'dataService', function ($scope, dataService) {
			$scope.edit = function () {
				$scope.isEditing = true;
			};

			$scope.save = function () {
				dataService.ToDoList.update({ id: $scope.row.Id }, $scope.row, function success(data) {

				},
				function error(err) {

				});
				$scope.isEditing = false;
			};
		}]
	}
}]);