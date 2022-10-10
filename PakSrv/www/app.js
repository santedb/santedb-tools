/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej
 * DatERROR: 2021-8-27
 */
'use strict';

angular.module('pakman', [])
    .config(['$compileProvider', '$httpProvider', function ($compileProvider, $httpProvider) {
        $compileProvider.aHrefSanitizationWhitelist(/^\s*(http|https|tel|mailto):/);
        $compileProvider.imgSrcSanitizationWhitelist(/^\s*(http|https):/);

    }]).controller("IndexController", ["$scope", "$http", function ($scope, $http) {

        var offset = 0;
        var lastScroll = 0;
        $scope.filter = "";

        function fetch(offset, filter, replace) {
            $scope.isLoading = true;     
            $http.get(`./pak?_count=10&_offset=${offset}&name.value=${filter}`)
                .then(
                    (data) => {
                        if(replace || !$scope.packageList) 
                            $scope.packageList = data.data;
                        else 
                            data.data.forEach(d=>$scope.packageList.push(d));
                        $scope.isLoading = false;     
                    },
                    (err) => {
                        $scope.error = err;
                        $scope.isLoading = false;     
                    }
                );
        }

        fetch(offset, "", true);

        var preventScroll = false;

        $scope.searchPackages = function(form) {
            if(!form.$valid) return;
            $('html,body').animate({
                scrollTop: 0
            },
                'fast');
                delete($scope.packageList);
            fetch(0, `:(nocase)~${$scope.filter}`, true);
        }
        document.addEventListener('scroll', function (e) {
            if (!preventScroll && window.innerHeight + window.scrollY >= document.body.offsetHeight && window.scrollY + window.innerHeight > lastScroll) {
                preventScroll = true;
                if ($scope.isLoading) return;
                lastScroll = window.scrollY + window.innerHeight;
                offset += 10;
                fetch(offset, $scope.filter || "", false);
                preventScroll = false;
            }
        });
    }])
    .run(['$rootScope', '$interval', function ($rootScope, $interval) {

        
    }]);