using Microsoft.AspNetCore.Mvc;
using Rtbs.Bloxcross.Models;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class VendorController : ControllerBase
{

    private readonly IBloxService _service;

    public VendorController(IBloxService service)
    {
        _service = service;
    }

    [HttpPost("new_vendor")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorRequest request)
    {
        var result = await _service.PostAsync("/vendors/new_vendor", request);
        return ApiResponseFactory.FromUpstream(result);
    }

    [HttpPost("update_vendor/{clientVendorId}")]
    public async Task<IActionResult> UpdateVendor(string clientVendorId, [FromBody] UpdateVendorRequest request)
    {
        var result = await _service.PostAsync($"/vendors/update_vendor/{clientVendorId}", request);
        return ApiResponseFactory.FromUpstream(result);
    }

    [HttpPost("all_vendor_payment_methods")]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var result = await _service.PostAsync("/payments/all_vendor_payment_methods", new { });
        return ApiResponseFactory.FromUpstream(result);
    }

    [HttpPost("new_vendor_account")]
    public async Task<IActionResult> CreateVendorAccount([FromBody] NewVendorAccountRequest request)
    {
        var result = await _service.PostAsync("/vendors/new_vendor_account", request);
        return ApiResponseFactory.FromUpstream(result);
    }
    [HttpPost("update_vendor_account/{vendorAccountId}")]
    public async Task<IActionResult> UpdateVendorAccount(string vendorAccountId, [FromBody] NewVendorAccountRequest request)
    {
        var result = await _service.PostAsync($"/vendors/update_vendor_account/{vendorAccountId}", request);
        return ApiResponseFactory.FromUpstream(result);
    }

    [HttpPost("pay_vendor")]
    public async Task<IActionResult> PayVendor([FromBody] PayVendorRequest request)
    {
        var result = await _service.PostAsync("/vendors/pay_vendor", request);
        return ApiResponseFactory.FromUpstream(result);
    }
   
}
