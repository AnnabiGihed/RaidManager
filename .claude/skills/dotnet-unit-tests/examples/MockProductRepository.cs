using System.Linq.Expressions;
using Moq;
using {Solution}.Domain.Features.Products.Aggregates;
using {Solution}.Domain.Features.Products.Repositories;

namespace {Solution}.Shared.UnitTests.Mocks;

/// <summary>
/// Author      : {Author from git config user.name}
/// Date        : {MM-yyyy}
/// Purpose     : Shared factory for the mocked <see cref="IProductRepository"/> used by every product feature.
///              The repository interface extends Pivot's <c>IAsyncCommandRepository&lt;Product, ProductId&gt;</c>;
///              the mock evaluates predicates against a caller-owned list so uniqueness rules behave realistically
///              without any database or in-memory EF provider.
/// </summary>
public static class MockProductRepository
{
	#region Public Methods
	/// <summary>
	/// Creates a repository mock backed by <paramref name="products"/>.
	/// </summary>
	/// <param name="products">The list that plays the role of persisted products; the caller may add to it.</param>
	/// <returns>A configured <see cref="Mock{T}"/> of <see cref="IProductRepository"/>.</returns>
	public static Mock<IProductRepository> Create(List<Product> products)
	{
		ArgumentNullException.ThrowIfNull(products);

		var mock = new Mock<IProductRepository>(MockBehavior.Strict);

		mock.Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Expression<Func<Product, bool>> predicate, CancellationToken _) => products.AsQueryable().Any(predicate));

		mock.Setup(r => r.FindByIdAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((ProductId id, CancellationToken _) => products.SingleOrDefault(p => p.Id == id));

		mock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
			.Callback((Product product, CancellationToken _) => products.Add(product))
			.Returns(Task.CompletedTask);

		return mock;
	}
	#endregion
}
