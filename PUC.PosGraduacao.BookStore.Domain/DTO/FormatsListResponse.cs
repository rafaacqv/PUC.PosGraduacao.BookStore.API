﻿using PUC.PosGraduacao.BookStore.Domain.Models;

namespace PUC.PosGraduacao.BookStore.Domain.DTO
{
  public class FormatsListResponse : BaseResponse
  {
    public List<Format> Formats { get; set; } = new List<Format>();
  }
}
