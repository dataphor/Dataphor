var app = angular.module('dataphoria', [
  'ngRoute',
  'dataphoriaDirectives',
  'dataphoriaServices',
  'dataphoriaControllers',
]);

app.config(['$routeProvider', function ($routeProvider) {
    	$routeProvider.
			when('/', {
				templateUrl: 'partials/home/index.html'
			});
    }]);