using System.Globalization;
using Moq;
using Reqnroll;
using Shouldly;
using Pivot.Framework.Domain.Shared;
using {Solution}.Domain.Features.Products.Aggregates;
using {Solution}.Domain.Features.Products.Events;
using {Solution}.Domain.Features.Products.Repositories;
using {Solution}.Shared.UnitTests.Mocks;

namespace {Solution}.Domain.UnitTests.Features.Products.Aggregates;

/// <summary>
/// Author      : {Author from git config user.name}
/// Date        : {MM-yyyy}
/// Purpose     : Step definitions for the "Product creation" feature. Arranges a product draft, creates the
///              <see cref="Product"/> aggregate through its real factory method against a mocked
///              <see cref="IProductRepository"/>, and asserts the returned Pivot <see cref="Result{TValue}"/>,
///              the raised domain event and the exact domain error messages.
/// </summary>
[Binding]
[Scope(Feature = "Product creation")]
public sealed class ProductCreationStepDefinitions
{
	#region Fields
	/// <summary>SKU of the product draft under test.</summary>
	private string _sku = string.Empty;

	/// <summary>Name of the product draft under test.</summary>
	private string _name = string.Empty;

	/// <summary>Unit price of the product draft under test.</summary>
	private decimal _unitPrice;

	/// <summary>Category of the product draft under test.</summary>
	private string _category = string.Empty;

	/// <summary>Result returned by <see cref="Product.CreateAsync"/>, captured by the When step.</summary>
	private Result<Product>? _creationResult;

	/// <summary>In-memory list backing the mocked repository, so uniqueness rules see previously created products.</summary>
	private readonly List<Product> _existingProducts = [];

	/// <summary>Mocked product repository from the shared mock factory.</summary>
	private readonly Mock<IProductRepository> _productRepository;
	#endregion

	#region Constructors
	/// <summary>
	/// Initialises a new <see cref="ProductCreationStepDefinitions"/> with the shared repository mock.
	/// </summary>
	public ProductCreationStepDefinitions()
	{
		_productRepository = MockProductRepository.Create(_existingProducts);
	}
	#endregion

	#region Given Steps
	/// <summary>Sets every attribute of the product draft from a vertical field/value table.</summary>
	/// <param name="details">Table with the columns <c>field</c> and <c>value</c>.</param>
	[Given("a product draft with these details")]
	public void GivenAProductDraftWithTheseDetails(DataTable details)
	{
		var values = details.Rows.ToDictionary(row => row["field"], row => row["value"]);
		_sku = values["sku"];
		_name = values["name"];
		_unitPrice = decimal.Parse(values["unit price"], CultureInfo.InvariantCulture);
		_category = values["category"];
	}

	/// <summary>Ensures the catalog holds no product with the given SKU.</summary>
	/// <param name="sku">The SKU that must be free.</param>
	[Given("no product exists with sku {string}")]
	public void GivenNoProductExistsWithSku(string sku)
	{
		_existingProducts.RemoveAll(product => product.Sku == sku);
	}

	/// <summary>Clears the SKU of the draft to exercise the required-value rule.</summary>
	[Given("the product sku is empty")]
	public void GivenTheProductSkuIsEmpty()
	{
		_sku = string.Empty;
	}

	/// <summary>Replaces the SKU of the draft.</summary>
	/// <param name="sku">The SKU from the scenario.</param>
	[Given("the product sku is {string}")]
	public void GivenTheProductSkuIs(string sku)
	{
		_sku = sku;
	}

	/// <summary>Replaces the unit price of the draft.</summary>
	/// <param name="unitPrice">The unit price from the scenario, in invariant culture.</param>
	[Given("the product unit price is {string}")]
	public void GivenTheProductUnitPriceIs(string unitPrice)
	{
		_unitPrice = decimal.Parse(unitPrice, CultureInfo.InvariantCulture);
	}

	/// <summary>Creates a product with the given SKU through the real factory so duplicate detection can be exercised.</summary>
	/// <param name="sku">The SKU already in use.</param>
	[Given("a product already exists with sku {string}")]
	public async Task GivenAProductAlreadyExistsWithSku(string sku)
	{
		var existing = await Product.CreateAsync(sku, "An existing valid product", 9.99m, "Beverages", _productRepository.Object, CancellationToken.None);
		_existingProducts.Add(existing.Value);   // .Value throws if the arrangement itself is invalid — Given steps don't assert
	}
	#endregion

	#region When Steps
	/// <summary>Creates the product through the aggregate factory method and captures the result.</summary>
	[When("the catalog manager creates the product")]
	public async Task WhenTheCatalogManagerCreatesTheProduct()
	{
		_creationResult = await Product.CreateAsync(_sku, _name, _unitPrice, _category, _productRepository.Object, CancellationToken.None);
	}
	#endregion

	#region Then Steps
	/// <summary>Asserts that the creation succeeded.</summary>
	[Then("the product is created")]
	public void ThenTheProductIsCreated()
	{
		CreationResult.IsSuccess.ShouldBeTrue();
	}

	/// <summary>Asserts that exactly one <see cref="ProductCreatedDomainEvent"/> was raised for the new product.</summary>
	[Then("a product created event is raised")]
	public void ThenAProductCreatedEventIsRaised()
	{
		var raised = CreationResult.Value.GetDomainEvents().OfType<ProductCreatedDomainEvent>().ShouldHaveSingleItem();
		raised.ProductId.ShouldBe(CreationResult.Value.Id.Value);
	}

	/// <summary>Asserts that the product received a non-empty strongly typed identifier.</summary>
	[Then("the product receives an identifier")]
	public void ThenTheProductReceivesAnIdentifier()
	{
		CreationResult.Value.Id.ShouldNotBeNull();
		CreationResult.Value.Id.Value.ShouldNotBe(Guid.Empty);
	}

	/// <summary>Asserts that the creation failed with exactly the expected domain error message.</summary>
	/// <param name="expectedMessage">The exact message from the domain error catalogue.</param>
	[Then("the product creation fails with the error {string}")]
	public void ThenTheProductCreationFailsWithTheError(string expectedMessage)
	{
		CreationResult.IsFailure.ShouldBeTrue();
		CreationResult.Error.Message.ShouldBe(expectedMessage);
	}
	#endregion

	#region Private Helpers
	/// <summary>Gets the captured creation result, failing the scenario if the When step did not run.</summary>
	private Result<Product> CreationResult =>
		_creationResult ?? throw new InvalidOperationException("The When step 'the catalog manager creates the product' did not run.");
	#endregion
}
