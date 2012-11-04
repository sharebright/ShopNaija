<%@ Page Language="C#" Inherits="System.Web.Mvc.ViewPage" %>

<html>
<head>
    <title>Coming Soon...</title>
    <style>
        body
        {
            margin: 0 20px 0 0;
            font-family: "Trebuchet MS", Verdana, Arial;
        }
        #bottom
        {
            position: fixed;
            bottom: 0;
        }
        #top, #bottom
        {
            background-color: #cba;
            height: 20px;
        }
        #top, #middle, #bottom
        {
            padding: 5px 5px 7px 15px;
            margin-right: 0px;
            width: 100%;
        }
    </style>
</head>
<body>
    <div id="top">
        top</div>
    <div id="middle">
        Coming Soon to Lagos...
    </div>
    <div id="bottom">
        bottom</div>
</body>
</html>
