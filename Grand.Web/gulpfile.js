//load modules
var gulp = require('gulp');
var uglify = require('gulp-uglify');
var cssmin = require('gulp-cssmin');
var del = require('del');

//root directory for all minified or copied files
var root = "./wwwroot/Minified/";

//paths for GrandNode .js files
var js3 = './wwwroot/Scripts/public.accordion.js/';
var js4 = './wwwroot/Scripts/public.actions.js/';
var js5 = './wwwroot/Scripts/public.ajaxcart.js/';
var js6 = './wwwroot/Scripts/public.common.js/';
var js7 = './wwwroot/Scripts/public.onepagecheckout.js/';

//uglify and copy do new output directory
gulp.task('scripts', function () {
    return gulp.src([js3, js4, js5, js6, js7])
        .pipe(uglify())
        .pipe(gulp.dest(root + 'Scripts/'))
})

//copy all files 1st (.css files need /fonts)
gulp.task('files', function () {
    return gulp.src('./Themes/DefaultClean/Content/**/*.*')
        .pipe(gulp.dest(root + 'Themes/DefaultClean/Content'))
})

//paths for GrandNode .css files
var css2 = './Themes/DefaultClean/Content/css/ie.css';
var css3 = './Themes/DefaultClean/Content/css/print.css';
var css4 = './Themes/DefaultClean/Content/css/style.css';
var css5 = './Themes/DefaultClean/Content/css/style.rtl.css';

//copy, minify and override css files 2nd
gulp.task('css', function () {
    return gulp.src([css2, css3, css4, css5])
        .pipe(cssmin())
        .pipe(gulp.dest(root + 'Themes/DefaultClean/Content/css'))
})

gulp.task('minify:all', ['scripts', 'files', 'css'])

gulp.task('delete', function () {
    del([root]);
})