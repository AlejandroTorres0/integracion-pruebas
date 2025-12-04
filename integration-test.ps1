# ==============================================================================
# Integration Test Script
# ==============================================================================
# This script performs an end-to-end integration test of the following flow:
# 1. Get a product from the stock API.
# 2. Add the product to the shopping cart in the compras API.
# 3. Checkout the shopping cart in the compras API.
#
# The script will verify that the checkout process completes successfully and that
# the response contains the expected information (reservation ID, shipping ID, etc.).
# ==============================================================================

# --- Configuration ---
$tokenUrl = "https://keycloak.cubells.com.ar/realms/ds-2025-realm/protocol/openid-connect/token"
$comprasApiUrl = "http://localhost:5001"
$stockApiUrl = "http://localhost:5004"
$logisticaApiUrl = "http://localhost:5002"

# Client credentials
$stockApiClientId = "grupo-08"
$stockApiClientSecret = "248f42b5-7007-47d1-a94e-e8941f352f6f"
$comprasApiClientId = "grupo-10" # This is the client id for the frontend
$comprasApiClientSecret = "66ff9787-4fa5-46b3-b546-4ccbe604d233"
$comprasApiUser = "pepe@grillo.com"
$comprasApiPassword = "123456"

# --- Helper Functions ---
function Get-KeycloakToken {
    param(
        [string]$clientId,
        [string]$clientSecret,
        [string]$username,
        [string]$password
    )

    $body = @{
        client_id     = $clientId
    }

    if ($PSBoundParameters.ContainsKey('clientSecret')) {
        $body.grant_type = 'client_credentials'
        $body.client_secret = $clientSecret
    } else {
        $body.grant_type = 'password'
        $body.username = $username
        $body.password = $password
    }

    try {
        $response = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body
        return $response.access_token
    }
    catch {
        $clientInfo = if ($PSBoundParameters.ContainsKey('clientSecret')) { $clientId } else { $username }
        Write-Host "  [ERROR] Could not obtain token for '$clientInfo': $($_.Exception.Message)" -ForegroundColor Red
        throw "Failed to obtain token."
    }
}


# --- Test Steps ---
try {
    # 1. Register User in Compras API
    Write-Host "1. Registering user in Compras API..." -ForegroundColor Cyan
    $registerBody = @{
        FirstName = "Pepe"
        LastName  = "Grillo"
        Email     = $comprasApiUser
        Password  = $comprasApiPassword
        RepeatPassword = $comprasApiPassword
    } | ConvertTo-Json
    
    try {
        Invoke-RestMethod -Uri "$comprasApiUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
        Write-Host "   [OK] User registered or already exists." -ForegroundColor Green
    } catch {
        # If the user already exists, the API might return an error, which we can ignore.
        if ($_.Exception.Response.StatusCode.value__ -eq 400 -or $_.Exception.Response.StatusCode.value__ -eq 409) {
            Write-Host "   [OK] User already exists." -ForegroundColor Green
        } else {
            throw $_.Exception
        }
    }

    # 2. Get Stock API Token
    Write-Host "2. Requesting Token for Stock API..." -ForegroundColor Cyan
    $stockToken = Get-KeycloakToken -clientId $stockApiClientId -clientSecret $stockApiClientSecret
    Write-Host "   [OK] Token received." -ForegroundColor Green

    # 3. Get a Product from Stock API
    Write-Host "3. Getting a product from Stock API..." -ForegroundColor Cyan
    $headers = @{
        Authorization = "Bearer $stockToken"
    }
    $products = Invoke-RestMethod -Uri "$stockApiUrl/productos" -Method Get -Headers $headers
    
    if ($products.data.Count -eq 0) {
        throw "No products found in stock API."
    }
    
    $product = $products.data[0]
    Write-Host "   [OK] Product found: $($product.nombre) (ID: $($product.id))" -ForegroundColor Green

    # 4. Get Compras API Token
    Write-Host "4. Requesting Token for Compras API (User Token)..." -ForegroundColor Cyan
    $comprasToken = Get-KeycloakToken -clientId $comprasApiClientId -clientSecret $comprasApiClientSecret -username $comprasApiUser -password $comprasApiPassword
    Write-Host "   [OK] Token received." -ForegroundColor Green

    # 5. Add Product to Cart
    Write-Host "5. Adding product to cart in Compras API..." -ForegroundColor Cyan
    $headers = @{
        Authorization = "Bearer $comprasToken"
        "Content-Type" = "application/json"
    }
    $body = @{
        ProductId = $product.id
        Quantity = 1
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$comprasApiUrl/api/shopcart" -Method Post -Headers $headers -Body $body
    Write-Host "   [OK] Product added to cart." -ForegroundColor Green

    # 6. Checkout
    Write-Host "6. Checking out cart in Compras API..." -ForegroundColor Cyan
    $checkoutBody = @{
        DeliveryAddress = @{
            Street = "Calle Falsa"
            Number = "123"
            City = "Springfield" 
            State = "Springfield"
            Country = "USA"
            PostalCode = "12345"
        }
        TransportType = "road"
    } | ConvertTo-Json
    
    $checkoutResponse = Invoke-RestMethod -Uri "$comprasApiUrl/api/shopcart/checkout" -Method Post -Headers $headers -Body $checkoutBody
    
    # 7. Verify Checkout Response
    Write-Host "7. Verifying checkout response..." -ForegroundColor Cyan
    if ($checkoutResponse.ReservaId -gt 0 -and $checkoutResponse.ShippingId -gt 0 -and $checkoutResponse.ShippingCost -gt 0) {
        Write-Host "   [SUCCESS] Integration test passed!" -ForegroundColor Green
        Write-Host "   Reservation ID: $($checkoutResponse.ReservaId)"
        Write-Host "   Shipping ID: $($checkoutResponse.ShippingId)"
        Write-Host "   Shipping Cost: $($checkoutResponse.ShippingCost)"
    } else {
        throw "Checkout response is not valid.`n" + ($checkoutResponse | ConvertTo-Json -Depth 3)
    }
}
catch {
    Write-Host "  [FAIL] An error occurred during the test: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode.value__
        $responseBody = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($responseBody)
        $errorText = $reader.ReadToEnd()
        Write-Host "  Status Code: $statusCode" -ForegroundColor Yellow
        Write-Host "  Response: $errorText" -ForegroundColor Yellow
    }
    exit 1
}

exit 0
