using ShopDemo.Application.Interfaces;
using ShopDemo.Domain;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShopDemo.Infrastructure;

public sealed class JsonProductCatalog(HttpClient httpClient) : IInventoryCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private List<Product>? _products;

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureProductsLoadedAsync(cancellationToken);
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            return [.. _products!];
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task<Product?> GetProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        var products = await GetProductsAsync(cancellationToken);
        return products.FirstOrDefault(product =>
            string.Equals(product.Id, productId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Product> UpdateStockAsync(
        string productId,
        int stock,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        if (stock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stock), "Stock cannot be negative.");
        }

        await EnsureProductsLoadedAsync(cancellationToken);
        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            var index = _products!.FindIndex(product =>
                string.Equals(product.Id, productId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                throw new KeyNotFoundException($"Product '{productId}' was not found.");
            }

            var updatedProduct = _products[index] with { Stock = stock };
            _products[index] = updatedProduct;
            return updatedProduct;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task EnsureProductsLoadedAsync(CancellationToken cancellationToken)
    {
        if (_products is not null)
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_products is not null)
            {
                return;
            }

            Product[]? products;
            try
            {
                products = await httpClient.GetFromJsonAsync<Product[]>("data/products.json", JsonOptions, cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The product catalog JSON is invalid.", exception);
            }

            if (products is null || products.Length == 0)
            {
                throw new InvalidDataException("The product catalog must contain at least one product.");
            }

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var product in products)
            {
                if (product is null)
                {
                    throw new InvalidDataException("Product catalog contains a null product entry.");
                }

                try
                {
                    product.Validate();
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidDataException($"Product configuration contains an invalid product: {exception.Message}", exception);
                }

                if (!ids.Add(product.Id))
                {
                    throw new InvalidDataException($"Product catalog contains duplicate id '{product.Id}'.");
                }
            }

            _products = [.. products];
        }
        finally
        {
            _loadLock.Release();
        }
    }
}
