@workitem:1234
Feature: Product creation
	As a catalog manager
	I want to create products in the catalog
	So that the catalog holds the products the organization sells

	Background:
		Given a product draft with these details
			| field      | value                      |
			| sku        | PRD-0001                   |
			| name       | Default valid product name |
			| unit price | 19.99                      |
			| category   | Beverages                  |

	Rule: A valid product draft becomes a catalog product

		Scenario: Valid draft is added to the catalog
			Given no product exists with sku "PRD-0001"
			When the catalog manager creates the product
			Then the product is created
			And a product created event is raised
			And the product receives an identifier

	Rule: A product sku is required and has a minimum length

		Scenario: Draft without a sku is rejected
			Given the product sku is empty
			When the catalog manager creates the product
			Then the product creation fails with the error "Value for field product sku is required !"

		Scenario: Draft with a short sku is rejected
			Given the product sku is "A"
			When the catalog manager creates the product
			Then the product creation fails with the error "Value A for product sku is to short, minimum length of 4 is required !"

	Rule: A product sku is unique in the catalog

		Scenario: Draft with an existing sku is rejected
			Given a product already exists with sku "PRD-0001"
			When the catalog manager creates the product
			Then the product creation fails with the error "Value  PRD-0001 for product sku  already exists !"

	Rule: A product unit price is never negative

		Scenario: Draft with a negative unit price is rejected
			Given the product unit price is "-1.00"
			When the catalog manager creates the product
			Then the product creation fails with the error "Value  -1.00 for product unit price  should not be negative !"
