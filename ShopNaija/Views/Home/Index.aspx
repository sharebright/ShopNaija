<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="Content1" ContentPlaceHolderID="TitleContent" runat="server">
    Shop Naija - Bringing you what you want.
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        div#outline
        {
            margin: 15px 0 15px 0;
            border-top: solid 1px red;
            border-bottom: solid 1px red;
            float: left;
            width: 100%;
            padding: 0;
        }
        div#outline div.step div
        {
            padding: 5px 10px 5px 10px;
        }
        .shopping-img, .coming-soon
        {
            text-align: center;
        }
    </style>
    <div class="under-construction container_12">
        <div class="grid_12">
            <div class="coming-soon"  style="margin: 0 0 -20px 0;">
                <img src="../../Content/images/Description.png" alt="Shop Naija will provide you with trendy new clothes direct from Europe, delivered to our collection centre in Lagos waiting for you to collect them and take them home. All you have to do is keep one eye here as we will be providing this brand new service very soon. See the steps below to see how it will work and leave your email address here and we will send you a little reminder when we are ready to go live." />
<!--Shop Naija will provide you with trendy new clothes direct from Europe, delivered to our collection centre in Lagos waiting for you to collect them and take them home. All you have to do is keep one eye here as we will be providing this brand new service very soon. See the steps below to see how it will work and leave your email address here and we will send you a little reminder when we are ready to go live.-->
            </div>
        </div>
    </div>
    <div class="shopping-img" style="margin: 0 0 5px 0;">
        <img src="../../Content/images/3ShoppingLadies.jpg" height="200px" />
    </div>
    <div id="container" class="container_12">
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 1</div>
            <div class="description">
                Visit www.shopnaija.co.uk</div>
        </div>
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 2</div>
            <div class="description">
                Do your shopping</div>
        </div>
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 3</div>
            <div class="description">
                Confirm &amp; verify your order</div>
        </div>
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 4</div>
            <div class="description">
                Pay for despatch</div>
        </div>
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 5</div>
            <div class="description">
                Receive your items within 10 days*</div>
        </div>
        <div class="item grid_1_and_A_half">
            <div class="number">
                Step 6</div>
            <div class="description">
                Happy customer. Repeat purchase.</div>
        </div>
    </div>
    <script type="text/javascript">
        $('document').ready(function () {

        });
    </script>
</asp:Content>
