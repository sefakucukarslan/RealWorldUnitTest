using Microsoft.AspNetCore.Mvc;
using Moq;
using RealWorldUnitTest.Web.Controllers;
using RealWorldUnitTest.Web.Models;
using RealWorldUnitTest.Web.Repository;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RealWorldUnitTest.Test
{
    public class ProductControllerTest
    {        
        private readonly Mock<IRepository<Product>> _mockRepo;
        private readonly ProductsController _controller;
        private List<Product> products; 

        public ProductControllerTest()
        {
            //_mockRepo = new Mock<IRepository<Product>>(MockBehavior.Strict); mocklama işleminde değerleri kesin olarak göndermek istersek bu şekilde tanımlarız.
            //_mockRepo = new Mock<IRepository<Product>>(MockBehavior.Loose); işlemi ise değerleri kesin olarak girmeni engelliyor yani gevşek davranmasını sağlıyor
            //_mockRepo = new Mock<IRepository<Product>>(); bu yapının default hali de _mockRepo = new Mock<IRepository<Product>>(MockBehavior.Loose); budur
            //Controller tarafında bulunan metotların içerisinde _repository gördüğümüz her noktada mocklama işlemi gerçekleştirmeliyiz.
            _mockRepo = new Mock<IRepository<Product>>();
            _controller = new ProductsController(_mockRepo.Object);
            products = new List<Product>() { new Product { Id=1, Name="Kalem", Price=100, Stock=50, Color="Kırmızı"},
            new Product { Id=2, Name="Defter", Price=200, Stock=500, Color="Mavi"} };
        }

        [Fact]
        public async void Index_ActionExecutes_ReturnView()
        {
            //Controllerda ki Index'in View'ini kontrol ediyoruz.
            var result = await _controller.Index();

            //IsType generic bir metottur. içerisine aldığı ifadenin doğru olup olmadığını kontrol eder.
            //örneğin:Assert.IsType<string>("Sefa"); tipi string değeri de string bir değer olduğu için true dönecektir.
            Assert.IsType<ViewResult>(result); //View'in dönüş tipi ViewResult olduğu için <ViewResult> yazdık.
        }

        [Fact]
        public async void Index_ActionExecutes_ReturnProductList()
        {
            _mockRepo.Setup(repo => repo.GetAll()).ReturnsAsync(products);
            //Controllerda ki GetAll metodu veritabanından bilgileri alacaktı
            //ama mocklama işlemi ile beraber buna gerek kalmıyor bu yüzden işlemlerimiz hızlanmış oluyor.
            //mock constructorda ki products'a atanan verileri kullanacak.
            var result = await _controller.Index();
            var viewResult = Assert.IsType<ViewResult>(result);//Viewden dönen değeri kontrol ediyoruz.

            var productList = Assert.IsAssignableFrom<IEnumerable<Product>>(viewResult.Model);//viewResult üzerinden modele erişiyoruz.
            //GetAll metodu geriye IEnumerable<Product> döndüğünden dolayı IEnumerable<Product>'a atanabilir bir data veriyoruz.
            
            Assert.Equal<int>(2, productList.Count());//<int> integer tipi test edeceğimizi belirtiyoruz.
            //2 olmasının sebebi constructorda verdiğimiz 2 değerden kaynaklanıyor.Çünkü mocklama o 2 değeri kullanıyor veritabanına bağlanmıyor.
        }

        [Fact]
        public async void Details_IdIsNull_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Details(null);
            //Details metodu parametre olarak null değeri alması durumunda Index sayfasına yönlendirilecek. Bunu test ediyoruz.

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            //Controller'da bulunan Details metodunda ki RedirectToAction'ın dönüş tipi RedirectToActionResult olduğu için bu şekilde tanımladık.

            Assert.Equal("Index", redirect.ActionName);
        }
        [Fact]
        public async void Details_IdInValid_ReturnNotFound()
        {
            //Controllerda ki Details metodunda GetById için bi parametre gönderiliyor ve gönderilen bu parametreye göre veri getirecek.
            //listede olmayan bir id parametresi verdiğimizde geriye null değeri dönecektir ve if bloğuna takılıp notfound hatası verecektir.
            //0 değeri vererek test işlemi gerçekleştiriyoruz.
            //Product nesnesi oluşturmamızın sebebi ise ReturnsAsync dönüş tipi Product nesnesi olduğu için. Normal null yazdığımızda kabul etmiyor.

            Product product = null;
            _mockRepo.Setup(x => x.GetById(0)).ReturnsAsync(product);

            var result = await _controller.Details(0);

            var redirect = Assert.IsType<NotFoundResult>(result);//result'ın gerçekleşen sonucu.

            Assert.Equal<int>(404, redirect.StatusCode);
        }

        [Theory]
        [InlineData(1)]
        public async void Details_ValidId_ReturnProduct(int productId)
        {
            //constructor'da oluşturduğumuz Liste den, parametreden göndereceğimiz 1 değerini listede ki id=1 ile birbirine eşitse producta ata.
            Product product = products.First(x => x.Id == productId);

            //GetById metodunu taklit et. ReturnsAsync geriye dönüş tipi Product olduğu için o nesneye bağlı değeri atamalıyız.
            //Burası Details metodunu çalıştırmadan işlem gerçekleştiriyor.
            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            //Parametre gönderilip Details metodu çalıştırılır ve sonucu resulta atar.
            var result = await _controller.Details(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            //Assert.IsAssignableFrom<Product>(viewResult.Model) burada parantez içinde ki değer mutlaka Product'tan miras alınmış olması gerekiyor demektir.
            //kısaca parantez içerisinde ki değer Generic içerisinde ki değere atanabilir mi ? atanabilirse true atanamazsa false döner.
            var resultProduct = Assert.IsAssignableFrom<Product>(viewResult.Model);

            Assert.Equal(product.Id, resultProduct.Id);
            Assert.Equal(product.Name, resultProduct.Name);
        }

        [Fact]
        public void Create_ActionExecutes_ReturnView()
        {
            var result = _controller.Create();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async void CreatePOST_InValidModelState_ReturnView()
        {
            //Name değerine Name alanı gereklidir değerini girdik.
            _controller.ModelState.AddModelError("Name", "Name alanı gereklidir");

            //products listesinde ki 1.listeyi ekledik.
            var result = await _controller.Create(products.First());

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsType<Product>(viewResult.Model);
        }

        [Fact]
        public async void CreatePOST_ValidModelState_ReturnRedirectToIndexAction()
        {
            var result = await _controller.Create(products.First());

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async void CreatePOST_InValidModelState_NeverCreateExecute()
        {
            //Name alanına boş bir değer gönderdiğimizde hata almalı
            _controller.ModelState.AddModelError("Name", "");

            //Create metoduna değeri gönderdik. Ancak Create metodunun çalışmaması lazım. çalışmayacağı için direk return View(product); ' a geçmeli.
            var result = await _controller.Create(products.First());

            //Mocklama yaparak Create metodunun çalışıp çalışmadığını doğruluyoruz.
            _mockRepo.Verify(repo => repo.Create(It.IsAny<Product>()),Times.Never());
        }

        [Fact]
        public async void Edit_IdIsNull_RedirectToIndexAction()
        {
            //Bu bölümde Edit metodunun id si null olması durumunda Index metoduna gidip gitmediğini test ettik.

            var result = await _controller.Edit(null);

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);

        }

        [Theory]
        [InlineData(3)]
        public async void Edit_IdInvalid_ReturnNotFound(int productId)
        {
            //Controller da bulunan Edit metodunun, GetById metoduna null değeri dönmesi durumunda geriye NotFound dönüp dönmediğini test ettik.

            Product product = null;
            _mockRepo.Setup(x => x.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Edit(productId);

            var redirect = Assert.IsType<NotFoundResult>(result);

            Assert.Equal(404, redirect.StatusCode);
        }

        [Theory]
        [InlineData(2)]
        public async void Edit_ActionExecutes_ReturnProduct(int productId)
        {
            //Controller da bulunan Edit metodunun, GetById metoduna 2 değerini verip sonucunun return View(product); dönmesini test ettik.

            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Edit(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            var resultProduct = Assert.IsAssignableFrom<Product>(viewResult.Model);

            Assert.Equal(product.Id, resultProduct.Id);

            Assert.Equal(product.Name, resultProduct.Name);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_IdIsNotEqualProduct_ReturnNotFound(int productId)
        {
            //Controller içerisinde bulunan Edit metoduna parametre olarak gonderilen değer ile product listesinde bulunan değeri test ettik.
            //iki değer birbirine eşit değilse NotFound dönmeli.

            var result = _controller.Edit(2,products.First(x => x.Id == productId));//burada 2 değerini vermezsek NotFound hatası vermez bizim amacımız hata verdirmek.

            var redirect = Assert.IsType<NotFoundResult>(result);

            Assert.Equal(404, redirect.StatusCode);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_InValidModelState_ReturnView(int productId)
        {
            //Controllerda ki Edit metoduna Model değerini yanlış gönderdiğimizde return View(product); dönüşünü test ettik.

            _controller.ModelState.AddModelError("Name", "");

            var result = _controller.Edit(productId, products.First(x => x.Id == productId));

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsType<Product>(viewResult.Model);//return View(product); olduğu için product değerlerinin tipini kontrol ettik.
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_ValidModelState_ReturnRedirectToIndexAction(int productId)
        {
            //ModelState'in geçerli olması durumunda Index metoduna geçip geçmediğini test ettik.

            var result = _controller.Edit(productId, products.First(x => x.Id == productId));

            var redirect = Assert.IsType<RedirectToActionResult>(result);

            Assert.Equal("Index", redirect.ActionName);
        }

        [Theory]
        [InlineData(1)]
        public void EditPOST_ValidModelState_UpdateMethodExecute(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.Update(product));

            _controller.Edit(productId, product);

            _mockRepo.Verify(repo => repo.Update(It.IsAny<Product>()),Times.Once());
        }

        [Fact]
        public async void Delete_IdIsNull_ReturnNotFound()
        {
            var result = await _controller.Delete(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(0)]
        public async void Delete_IdIsNotEqualProduct_ReturnNotFound(int productId)
        {
            Product product = null;
            
            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Delete(productId);

            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [InlineData(1)]
        public async void Delete_ActionExecutes_ReturnProduct(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.GetById(productId)).ReturnsAsync(product);

            var result = await _controller.Delete(productId);

            var viewResult = Assert.IsType<ViewResult>(result);

            Assert.IsAssignableFrom<Product>(viewResult.Model);
        }

        [Theory]
        [InlineData(1)]
        public async void DeleteConfirmed_ActionExecutes_ReturnRedirectToIndexAction(int productId)
        {
            var result = await _controller.DeleteConfirmed(productId);

            Assert.IsType<RedirectToActionResult>(result);
        }

        [Theory]
        [InlineData(1)]
        public async void DeleteConfirmed_ActionExecutes_DeleteMethodExecute(int productId)
        {
            var product = products.First(x => x.Id == productId);

            _mockRepo.Setup(repo => repo.Delete(product));

            await _controller.DeleteConfirmed(productId);

            _mockRepo.Verify(repo => repo.Delete(It.IsAny<Product>()),Times.Once());
        }

    }
}
