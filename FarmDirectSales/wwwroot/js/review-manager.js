/**
 * 评价管理工具
 * @description 处理产品评价的显示、回复和管理功能
 */

const ReviewManager = {
    /**
     * 获取产品的评价列表
     * @param {number} productId - 产品ID
     * @returns {Promise<Array>} 评价列表
     */
    async getProductReviews(productId) {
        try {
            const response = await http.get(`/api/review/product/${productId}`);
            return response.data || [];
        } catch (error) {
            console.error('获取产品评价失败:', error);
            return [];
        }
    },
    
    /**
     * 获取农户的所有产品评价
     * @param {number} farmerId - 农户ID
     * @returns {Promise<Array>} 评价列表
     */
    async getFarmerReviews(farmerId) {
        try {
            const response = await http.get(`/api/review/farmer/${farmerId}`);
            return response.data || [];
        } catch (error) {
            console.error('获取农户评价失败:', error);
            return [];
        }
    },
    
    /**
     * 添加评价回复
     * @param {number} reviewId - 评价ID
     * @param {string} replyContent - 回复内容
     * @returns {Promise<object>} 回复结果
     */
    async addReply(reviewId, replyContent) {
        try {
            const response = await http.post(`/api/review/${reviewId}/reply`, {
                content: replyContent
            });
            return response;
        } catch (error) {
            console.error('添加评价回复失败:', error);
            throw error;
        }
    },
    
    /**
     * 修改评价回复
     * @param {number} reviewId - 评价ID
     * @param {number} replyId - 回复ID
     * @param {string} replyContent - 回复内容
     * @returns {Promise<object>} 回复结果
     */
    async updateReply(reviewId, replyId, replyContent) {
        try {
            const response = await http.put(`/api/review/${reviewId}/reply/${replyId}`, {
                content: replyContent
            });
            return response;
        } catch (error) {
            console.error('更新评价回复失败:', error);
            throw error;
        }
    },
    
    /**
     * 删除评价回复
     * @param {number} reviewId - 评价ID
     * @param {number} replyId - 回复ID
     * @returns {Promise<object>} 结果
     */
    async deleteReply(reviewId, replyId) {
        try {
            const response = await http.delete(`/api/review/${reviewId}/reply/${replyId}`);
            return response;
        } catch (error) {
            console.error('删除评价回复失败:', error);
            throw error;
        }
    },
    
    /**
     * 举报不当评价
     * @param {number} reviewId - 评价ID
     * @param {string} reason - 举报理由
     * @returns {Promise<object>} 结果
     */
    async reportReview(reviewId, reason) {
        try {
            const response = await http.post(`/api/review/${reviewId}/report`, {
                reason: reason
            });
            return response;
        } catch (error) {
            console.error('举报评价失败:', error);
            throw error;
        }
    },
    
    /**
     * 渲染星级评分
     * @param {number} rating - 评分值(1-5)
     * @param {boolean} readOnly - 是否只读
     * @returns {string} HTML字符串
     */
    renderStarRating(rating, readOnly = true) {
        let starsHtml = '';
        for (let i = 1; i <= 5; i++) {
            if (i <= rating) {
                starsHtml += `<i class="fas fa-star text-warning"></i>`;
            } else {
                starsHtml += `<i class="far fa-star text-warning"></i>`;
            }
        }
        return starsHtml;
    },
    
    /**
     * 渲染评价列表
     * @param {Array} reviews - 评价数据
     * @param {string} containerId - 容器元素ID
     * @param {boolean} showReplyForm - 是否显示回复表单
     * @param {boolean} isFarmer - 是否为农户视角
     */
    renderReviews(reviews, containerId, showReplyForm = false, isFarmer = false) {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        if (!reviews || reviews.length === 0) {
            container.innerHTML = '<div class="alert alert-info">暂无评价</div>';
            return;
        }
        
        let html = '';
        
        reviews.forEach(review => {
            const reviewDate = new Date(review.createTime).toLocaleDateString('zh-CN');
            const productLink = review.product ? 
                `<a href="/pages/product-detail.html?id=${review.product.productId}" class="text-decoration-none">${review.product.productName}</a>` :
                '未知产品';
            
            html += `
                <div class="card mb-4 review-card" data-review-id="${review.reviewId}">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <div>
                            <span class="fw-bold">${review.isAnonymous ? '匿名用户' : review.user.username}</span>
                            <span class="text-muted ms-2">${reviewDate}</span>
                        </div>
                        <div class="star-rating">
                            ${this.renderStarRating(review.rating)}
                        </div>
                    </div>
                    <div class="card-body">
                        <div class="mb-3">
                            ${isFarmer ? `<div class="text-muted mb-2">评价产品: ${productLink}</div>` : ''}
                            <p class="card-text">${review.content}</p>
                            ${review.images && review.images.length > 0 ? 
                                `<div class="review-images mt-2 d-flex flex-wrap">
                                    ${review.images.map(img => `
                                        <a href="${img}" data-lightbox="review-${review.reviewId}" class="me-2 mb-2">
                                            <img src="${img}" alt="评价图片" class="img-thumbnail" style="width: 80px; height: 80px; object-fit: cover;">
                                        </a>
                                    `).join('')}
                                </div>` 
                                : ''}
                        </div>
                        
                        ${review.replies && review.replies.length > 0 ? 
                            `<div class="replies mt-3">
                                ${review.replies.map(reply => {
                                    const replyDate = new Date(reply.createTime).toLocaleDateString('zh-CN');
                                    return `
                                        <div class="reply p-3 rounded bg-light mb-2" data-reply-id="${reply.replyId}">
                                            <div class="d-flex justify-content-between">
                                                <div>
                                                    <span class="fw-bold">商家回复</span>
                                                    <span class="text-muted ms-2">${replyDate}</span>
                                                </div>
                                                ${isFarmer ? 
                                                    `<div class="reply-actions">
                                                        <button class="btn btn-sm btn-outline-primary edit-reply-btn" data-review-id="${review.reviewId}" data-reply-id="${reply.replyId}">
                                                            <i class="fas fa-edit"></i>
                                                        </button>
                                                        <button class="btn btn-sm btn-outline-danger delete-reply-btn" data-review-id="${review.reviewId}" data-reply-id="${reply.replyId}">
                                                            <i class="fas fa-trash"></i>
                                                        </button>
                                                    </div>` 
                                                    : ''}
                                            </div>
                                            <p class="mt-2 mb-0">${reply.content}</p>
                                        </div>
                                    `;
                                }).join('')}
                            </div>` 
                            : ''}
                        
                        ${showReplyForm && isFarmer ? 
                            review.replies && review.replies.length > 0 ?
                                `<button class="btn btn-sm btn-outline-success mt-3 add-reply-btn" data-review-id="${review.reviewId}">
                                    <i class="fas fa-reply me-1"></i> 追加回复
                                </button>` :
                                `<div class="reply-form mt-3">
                                    <textarea class="form-control reply-textarea" rows="2" placeholder="添加回复..."></textarea>
                                    <div class="d-flex justify-content-end mt-2">
                                        <button class="btn btn-sm btn-success submit-reply-btn" data-review-id="${review.reviewId}">
                                            <i class="fas fa-paper-plane me-1"></i> 提交回复
                                        </button>
                                    </div>
                                </div>`
                            : ''}
                    </div>
                    ${isFarmer ? 
                        `<div class="card-footer bg-transparent">
                            <div class="d-flex justify-content-end">
                                <button class="btn btn-sm btn-outline-danger report-review-btn" data-review-id="${review.reviewId}" data-bs-toggle="modal" data-bs-target="#reportReviewModal">
                                    <i class="fas fa-flag me-1"></i> 举报不当评价
                                </button>
                            </div>
                        </div>` 
                        : ''}
                </div>
            `;
        });
        
        container.innerHTML = html;
        
        // 绑定事件处理程序
        if (showReplyForm && isFarmer) {
            this.bindReviewEvents(container);
        }
    },
    
    /**
     * 绑定评价相关的事件处理
     * @param {HTMLElement} container - 容器元素
     */
    bindReviewEvents(container) {
        // 提交回复按钮点击事件
        container.querySelectorAll('.submit-reply-btn').forEach(button => {
            button.addEventListener('click', async (e) => {
                const reviewId = e.target.closest('.submit-reply-btn').dataset.reviewId;
                const replyTextarea = e.target.closest('.reply-form').querySelector('.reply-textarea');
                const replyContent = replyTextarea.value.trim();
                
                if (!replyContent) {
                    alert('请输入回复内容');
                    return;
                }
                
                try {
                    button.disabled = true;
                    button.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> 提交中...';
                    
                    await this.addReply(reviewId, replyContent);
                    
                    // 刷新评价列表
                    const productId = document.querySelector('[data-product-id]')?.dataset.productId;
                    const farmerId = document.querySelector('[data-farmer-id]')?.dataset.farmerId;
                    
                    if (productId) {
                        const reviews = await this.getProductReviews(productId);
                        this.renderReviews(reviews, container.id, true, true);
                    } else if (farmerId) {
                        const reviews = await this.getFarmerReviews(farmerId);
                        this.renderReviews(reviews, container.id, true, true);
                    }
                    
                    alert('回复成功');
                } catch (error) {
                    alert(`回复失败: ${error.message}`);
                } finally {
                    button.disabled = false;
                    button.innerHTML = '<i class="fas fa-paper-plane me-1"></i> 提交回复';
                }
            });
        });
        
        // 添加回复按钮点击事件
        container.querySelectorAll('.add-reply-btn').forEach(button => {
            button.addEventListener('click', (e) => {
                const reviewCard = e.target.closest('.review-card');
                const reviewId = reviewCard.dataset.reviewId;
                
                // 创建回复表单
                const replyFormHtml = `
                    <div class="reply-form mt-3">
                        <textarea class="form-control reply-textarea" rows="2" placeholder="添加回复..."></textarea>
                        <div class="d-flex justify-content-end mt-2">
                            <button class="btn btn-sm btn-outline-secondary me-2 cancel-reply-btn">
                                <i class="fas fa-times me-1"></i> 取消
                            </button>
                            <button class="btn btn-sm btn-success submit-reply-btn" data-review-id="${reviewId}">
                                <i class="fas fa-paper-plane me-1"></i> 提交回复
                            </button>
                        </div>
                    </div>
                `;
                
                // 替换按钮为表单
                button.insertAdjacentHTML('afterend', replyFormHtml);
                button.style.display = 'none';
                
                // 绑定取消回复按钮事件
                reviewCard.querySelector('.cancel-reply-btn').addEventListener('click', () => {
                    reviewCard.querySelector('.reply-form').remove();
                    button.style.display = 'inline-block';
                });
                
                // 绑定提交回复按钮事件
                reviewCard.querySelector('.submit-reply-btn').addEventListener('click', async (e) => {
                    const reviewId = e.target.dataset.reviewId;
                    const replyTextarea = reviewCard.querySelector('.reply-textarea');
                    const replyContent = replyTextarea.value.trim();
                    
                    if (!replyContent) {
                        alert('请输入回复内容');
                        return;
                    }
                    
                    try {
                        e.target.disabled = true;
                        e.target.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> 提交中...';
                        
                        await this.addReply(reviewId, replyContent);
                        
                        // 刷新评价列表
                        const productId = document.querySelector('[data-product-id]')?.dataset.productId;
                        const farmerId = document.querySelector('[data-farmer-id]')?.dataset.farmerId;
                        
                        if (productId) {
                            const reviews = await this.getProductReviews(productId);
                            this.renderReviews(reviews, container.id, true, true);
                        } else if (farmerId) {
                            const reviews = await this.getFarmerReviews(farmerId);
                            this.renderReviews(reviews, container.id, true, true);
                        }
                        
                        alert('回复成功');
                    } catch (error) {
                        alert(`回复失败: ${error.message}`);
                    }
                });
            });
        });
        
        // 编辑回复按钮点击事件
        container.querySelectorAll('.edit-reply-btn').forEach(button => {
            button.addEventListener('click', (e) => {
                const reviewId = button.dataset.reviewId;
                const replyId = button.dataset.replyId;
                const replyElement = button.closest('.reply');
                const replyContent = replyElement.querySelector('p').textContent;
                
                // 替换回复内容为编辑表单
                const originalHtml = replyElement.innerHTML;
                replyElement.innerHTML = `
                    <textarea class="form-control edit-reply-textarea" rows="2">${replyContent}</textarea>
                    <div class="d-flex justify-content-end mt-2">
                        <button class="btn btn-sm btn-outline-secondary me-2 cancel-edit-btn">
                            <i class="fas fa-times me-1"></i> 取消
                        </button>
                        <button class="btn btn-sm btn-success save-edit-btn" data-review-id="${reviewId}" data-reply-id="${replyId}">
                            <i class="fas fa-save me-1"></i> 保存
                        </button>
                    </div>
                `;
                
                // 绑定取消编辑按钮事件
                replyElement.querySelector('.cancel-edit-btn').addEventListener('click', () => {
                    replyElement.innerHTML = originalHtml;
                });
                
                // 绑定保存编辑按钮事件
                replyElement.querySelector('.save-edit-btn').addEventListener('click', async (e) => {
                    const reviewId = e.target.dataset.reviewId;
                    const replyId = e.target.dataset.replyId;
                    const editTextarea = replyElement.querySelector('.edit-reply-textarea');
                    const newContent = editTextarea.value.trim();
                    
                    if (!newContent) {
                        alert('回复内容不能为空');
                        return;
                    }
                    
                    try {
                        e.target.disabled = true;
                        e.target.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> 保存中...';
                        
                        await this.updateReply(reviewId, replyId, newContent);
                        
                        // 刷新评价列表
                        const productId = document.querySelector('[data-product-id]')?.dataset.productId;
                        const farmerId = document.querySelector('[data-farmer-id]')?.dataset.farmerId;
                        
                        if (productId) {
                            const reviews = await this.getProductReviews(productId);
                            this.renderReviews(reviews, container.id, true, true);
                        } else if (farmerId) {
                            const reviews = await this.getFarmerReviews(farmerId);
                            this.renderReviews(reviews, container.id, true, true);
                        }
                        
                        alert('修改成功');
                    } catch (error) {
                        alert(`修改失败: ${error.message}`);
                    }
                });
            });
        });
        
        // 删除回复按钮点击事件
        container.querySelectorAll('.delete-reply-btn').forEach(button => {
            button.addEventListener('click', async (e) => {
                if (!confirm('确定要删除这条回复吗？')) {
                    return;
                }
                
                const reviewId = button.dataset.reviewId;
                const replyId = button.dataset.replyId;
                
                try {
                    button.disabled = true;
                    
                    await this.deleteReply(reviewId, replyId);
                    
                    // 刷新评价列表
                    const productId = document.querySelector('[data-product-id]')?.dataset.productId;
                    const farmerId = document.querySelector('[data-farmer-id]')?.dataset.farmerId;
                    
                    if (productId) {
                        const reviews = await this.getProductReviews(productId);
                        this.renderReviews(reviews, container.id, true, true);
                    } else if (farmerId) {
                        const reviews = await this.getFarmerReviews(farmerId);
                        this.renderReviews(reviews, container.id, true, true);
                    }
                    
                    alert('删除成功');
                } catch (error) {
                    alert(`删除失败: ${error.message}`);
                    button.disabled = false;
                }
            });
        });
        
        // 举报评价按钮点击事件
        container.querySelectorAll('.report-review-btn').forEach(button => {
            button.addEventListener('click', (e) => {
                const reviewId = button.dataset.reviewId;
                const reportModal = document.getElementById('reportReviewModal');
                
                if (reportModal) {
                    const submitButton = reportModal.querySelector('.submit-report-btn');
                    submitButton.dataset.reviewId = reviewId;
                    
                    // 绑定举报提交按钮事件
                    submitButton.addEventListener('click', async () => {
                        const reasonTextarea = reportModal.querySelector('.report-reason');
                        const reason = reasonTextarea.value.trim();
                        
                        if (!reason) {
                            alert('请输入举报理由');
                            return;
                        }
                        
                        try {
                            submitButton.disabled = true;
                            submitButton.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i> 提交中...';
                            
                            await this.reportReview(reviewId, reason);
                            
                            // 关闭模态框
                            const bsModal = bootstrap.Modal.getInstance(reportModal);
                            bsModal.hide();
                            
                            // 清空输入
                            reasonTextarea.value = '';
                            
                            alert('举报成功，我们会尽快处理');
                        } catch (error) {
                            alert(`举报失败: ${error.message}`);
                        } finally {
                            submitButton.disabled = false;
                            submitButton.innerHTML = '<i class="fas fa-paper-plane me-1"></i> 提交举报';
                        }
                    });
                }
            });
        });
    }
};

// 导出为全局变量
window.ReviewManager = ReviewManager; 