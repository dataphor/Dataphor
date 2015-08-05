services.factory('dataService', ['$rootScope', '$http', '$resource', 'appSettings', function ($rootScope, $http, $resource, appSettings) {
	var
		_todolist = $resource(appSettings.serviceBase + '/ToDoList/:id', { id: '@id' }, {
			'query': { method: 'GET', isArray: false },
			'update': { method: 'PUT' }
		});

	return {
		ToDoList: _todolist
	};
}]);